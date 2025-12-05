package protocol

import (
	"bufio"
	"encoding/binary"
	"fmt"
	"io"
	"net"
	"os"
	"sync"
	"time"
)

type Client struct {
	conn                net.Conn
	reader              *bufio.Reader
	readMutex           sync.Mutex
	onFileUploaded      func(string)
	notificationRunning bool
	notificationStop    chan bool
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
	c.readMutex.Lock()

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

	if cmdByte[0] == CmdFileUploaded {
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
	} else {
		c.readMutex.Unlock()
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
	c.conn.Write([]byte{CmdGetFile})

	nameBytes := []byte(filename)
	WriteInt(c.conn, len(nameBytes))
	c.conn.Write(nameBytes)

	c.readMutex.Lock()
	size, err := readInt64FromReader(c.reader)
	c.readMutex.Unlock()
	if err != nil {
		return err
	}

	out, err := os.Create(savePath)
	if err != nil {
		return err
	}
	defer out.Close()

	buf := make([]byte, 8192)
	var received int64 = 0

	for received < size {
		c.readMutex.Lock()
		toRead := int64(len(buf))
		if toRead > size-received {
			toRead = size - received
		}
		n, err := c.reader.Read(buf[:toRead])
		c.readMutex.Unlock()
		if err != nil && err != io.EOF {
			return err
		}
		if n == 0 {
			break
		}
		out.Write(buf[:n])
		received += int64(n)
	}

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

