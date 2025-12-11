package protocol

import (
	"bufio"
	"encoding/binary"
	"fmt"
	"io"
	"net"
	"os"
	"sync"
	"sync/atomic"
	"time"
)

type Client struct {
	conn                net.Conn
	reader              *bufio.Reader
	readMutex           sync.Mutex
	onFileUploaded      func(string)
	notificationRunning bool
	notificationStop    chan bool
	downloadInProgress   int32 // Atomic flag: 1 = download in progress, 0 = not in progress
}

func NewClient(conn net.Conn) *Client {
	return &Client{
		conn:             conn,
		reader:           bufio.NewReaderSize(conn, 8192),
		notificationStop: make(chan bool),
	}
}

func (c *Client) SetOnFileUploadedCallback(callback func(string)) {
	c.onFileUploaded = callback
}

func (c *Client) StartNotificationListener() {
	if c.notificationRunning {
		return
	}
	c.notificationRunning = true

	go func() {
		ticker := time.NewTicker(100 * time.Millisecond)
		defer ticker.Stop()

		for {
			select {
			case <-c.notificationStop:
				c.notificationRunning = false
				return
			case <-ticker.C:
				c.checkForNotification()
			}
		}
	}()
}

func (c *Client) StopNotificationListener() {
	if c.notificationRunning {
		c.notificationStop <- true
	}
}

func (c *Client) checkForNotification() {
	// Completely skip if download is in progress - no stream operations at all
	if atomic.LoadInt32(&c.downloadInProgress) == 1 {
		return
	}

	// Process ALL available notifications in a loop
	for {
		c.readMutex.Lock()

		// Triple-check download status - must not be downloading
		if atomic.LoadInt32(&c.downloadInProgress) == 1 {
			c.readMutex.Unlock()
			return
		}

		// Check if data is available without blocking
		c.conn.SetReadDeadline(time.Now().Add(10 * time.Millisecond))
		
		cmdByte, err := c.reader.Peek(1)
		if err != nil {
			// No data available or timeout - reset deadline
			c.conn.SetReadDeadline(time.Time{})
			c.readMutex.Unlock()
			return
		}
		c.conn.SetReadDeadline(time.Time{})

		// Check download status again before processing
		if atomic.LoadInt32(&c.downloadInProgress) == 1 {
			c.readMutex.Unlock()
			return
		}

		if cmdByte[0] != CmdFileUploaded {
			// Not a notification, stop processing
			c.readMutex.Unlock()
			return
		}

		// Final check before reading notification
		if atomic.LoadInt32(&c.downloadInProgress) == 1 {
			c.readMutex.Unlock()
			return
		}

		// Read the notification
		c.reader.ReadByte() // consume the command byte
		
		nameLen, err := readIntFromReader(c.reader)
		if err != nil {
			c.readMutex.Unlock()
			return
		}

		nameBytes, err := readBytesFromReader(c.reader, nameLen)
		if err != nil {
			c.readMutex.Unlock()
			return
		}

		fileName := string(nameBytes)
		
		// Release lock before calling callback to avoid deadlock
		// The callback will need to acquire the lock for ListFiles/DownloadFile
		c.readMutex.Unlock()
		
		if c.onFileUploaded != nil {
			c.onFileUploaded(fileName)
		}
		
		// Continue loop to check for more notifications
	}
}

// Helper functions for reading from bufio.Reader
func readIntFromReader(reader *bufio.Reader) (int, error) {
	buf := make([]byte, 4)
	_, err := io.ReadFull(reader, buf)
	if err != nil {
		return 0, err
	}
	return int(binary.LittleEndian.Uint32(buf)), nil
}

func readInt64FromReader(reader *bufio.Reader) (int64, error) {
	buf := make([]byte, 8)
	_, err := io.ReadFull(reader, buf)
	if err != nil {
		return 0, err
	}
	return int64(binary.LittleEndian.Uint64(buf)), nil
}

func readBytesFromReader(reader *bufio.Reader, length int) ([]byte, error) {
	buf := make([]byte, length)
	_, err := io.ReadFull(reader, buf)
	return buf, err
}

func (c *Client) Auth(username, password string) error {
	c.conn.Write([]byte{CmdAuth})

	usernameBytes := []byte(username)
	WriteInt(c.conn, len(usernameBytes))
	c.conn.Write(usernameBytes)

	passwordBytes := []byte(password)
	WriteInt(c.conn, len(passwordBytes))
	c.conn.Write(passwordBytes)

	c.readMutex.Lock()
	defer c.readMutex.Unlock()

	result := make([]byte, 1)
	_, err := io.ReadFull(c.reader, result)
	if err != nil {
		return err
	}
	if result[0] != 1 {
		return fmt.Errorf("authentication failed")
	}
	return nil
}

func (c *Client) ListFiles() ([]string, error) {
	c.conn.Write([]byte{CmdListFiles})

	c.readMutex.Lock()
	defer c.readMutex.Unlock()

	count, err := readIntFromReader(c.reader)
	if err != nil {
		return nil, err
	}

	files := make([]string, 0, count)
	for i := 0; i < count; i++ {
		nameLen, err := readIntFromReader(c.reader)
		if err != nil {
			return nil, err
		}

		nameBytes, err := readBytesFromReader(c.reader, nameLen)
		if err != nil {
			return nil, err
		}

		files = append(files, string(nameBytes))
	}

	return files, nil
}

func (c *Client) DownloadFile(filename, savePath string) error {
	// Set download flag to prevent notification interference
	atomic.StoreInt32(&c.downloadInProgress, 1)
	defer atomic.StoreInt32(&c.downloadInProgress, 0)

	// Wait longer to ensure notification listener has seen the flag and completely stopped
	// This is critical - notification listener must not touch stream during downloads
	time.Sleep(300 * time.Millisecond)

	c.conn.Write([]byte{CmdGetFile})

	nameBytes := []byte(filename)
	WriteInt(c.conn, len(nameBytes))
	c.conn.Write(nameBytes)

	c.readMutex.Lock()
	// Double-check notification listener is not interfering
	// This ensures we have exclusive access to the stream
	size, err := readInt64FromReader(c.reader)
	c.readMutex.Unlock()
	if err != nil {
		return err
	}
	
	if size < 0 {
		return fmt.Errorf("file not found on server: %s", filename)
	}

	out, err := os.Create(savePath)
	if err != nil {
		return err
	}
	defer out.Close()

	buf := make([]byte, 8192)
	var received int64 = 0

	// Read file data without any notification checking
	for received < size {
		// Check download flag is still set (should be, but verify)
		if atomic.LoadInt32(&c.downloadInProgress) == 0 {
			return fmt.Errorf("download interrupted - flag was cleared unexpectedly. Received %d of %d bytes", received, size)
		}
		
		c.readMutex.Lock()
		
		// Calculate exactly how many bytes we need to read
		remaining := size - received
		toRead := int(remaining)
		if toRead > len(buf) {
			toRead = len(buf)
		}
		
		// Read exactly the amount we need, ensuring we don't stop early
		n, err := io.ReadFull(c.reader, buf[:toRead])
		c.readMutex.Unlock()
		
		if err != nil {
			if err == io.EOF || err == io.ErrUnexpectedEOF {
				return fmt.Errorf("unexpected end of stream. Expected %d bytes, received %d", size, received)
			}
			return err
		}
		
		if n == 0 {
			return fmt.Errorf("unexpected zero read. Expected %d bytes, received %d", size, received)
		}
		
		_, writeErr := out.Write(buf[:n])
		if writeErr != nil {
			return fmt.Errorf("failed to write file: %v", writeErr)
		}
		
		// Flush to ensure data is written to disk immediately
		out.Sync()
		
		received += int64(n)
	}

	// Final sync to ensure all data is written
	out.Sync()
	return nil
}

func (c *Client) UploadFile(filename string) error {
	file, err := os.Open(filename)
	if err != nil {
		return fmt.Errorf("failed to open file: %v", err)
	}
	defer file.Close()

	fileInfo, err := file.Stat()
	if err != nil {
		return err
	}

	c.conn.Write([]byte{CmdPutFile})

	nameBytes := []byte(fileInfo.Name())
	WriteInt(c.conn, len(nameBytes))
	c.conn.Write(nameBytes)

	WriteInt64(c.conn, fileInfo.Size())

	buf := make([]byte, 8192)
	_, err = io.CopyBuffer(c.conn, file, buf)
	return err
}

func (c *Client) Ping() (bool, error) {
	c.conn.Write([]byte{CmdPing})
	
	c.readMutex.Lock()
	defer c.readMutex.Unlock()

	resp := make([]byte, 1)
	_, err := io.ReadFull(c.reader, resp)
	if err != nil {
		return false, err
	}
	return resp[0] == 1, nil
}

