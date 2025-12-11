package client

import (
	"bufio"
	"fmt"
	"net"
	"os"
	"strings"
	"sync/atomic"
	"time"

	"GoClient/internal/commands"
	"GoClient/internal/protocol"
)

type Client struct {
	conn           net.Conn
	protocolClient *protocol.Client
	commandRegistry *commands.Registry
	authenticated  bool
	autoDownloadInProgress int32 // Atomic flag: 1 = auto-download in progress, 0 = not in progress
}

func NewClient(serverAddr string) (*Client, error) {
	conn, err := net.Dial("tcp", serverAddr)
	if err != nil {
		return nil, fmt.Errorf("failed to connect: %v", err)
	}

	protocolClient := protocol.NewClient(conn)
	
	client := &Client{
		conn:            conn,
		protocolClient:  protocolClient,
		commandRegistry: commands.NewRegistry(),
		authenticated:   false,
	}
	
	// Set up auto-download callback for notifications
	// Download ALL missing files when a notification arrives (same as download command)
	protocolClient.SetOnFileUploadedCallback(func(fileName string) {
		fmt.Printf("\n[Notification] File uploaded: %s\n", fileName)
		
		// Prevent concurrent auto-downloads
		if !atomic.CompareAndSwapInt32(&client.autoDownloadInProgress, 0, 1) {
			fmt.Printf("[Auto-download] Already downloading, queuing notification for: %s\n", fileName)
			return
		}
		
		// Execute downloads in a separate goroutine using the exact same mechanism as download command
		go func() {
			defer atomic.StoreInt32(&client.autoDownloadInProgress, 0)
			
			// Wait longer to ensure notification listener has fully released all locks
			// This is critical to prevent interference
			time.Sleep(500 * time.Millisecond)
			
			fmt.Println("[Auto-download] Downloading all missing files...")
			
			// Use the exact same code path as typing "download" command
			// This downloads ALL missing files, not just the one from notification
			handler, exists := client.commandRegistry.Get("download")
			if exists {
				ctx := &commands.Context{
					ProtocolClient: protocolClient,
					Authenticated:  &client.authenticated,
				}
				err := handler(ctx, []string{"download"})
				if err != nil {
					fmt.Printf("[Auto-download] Error: %v\n", err)
				}
			}
			fmt.Print("> ") // Restore prompt
		}()
	})
	
	// Start notification listener
	protocolClient.StartNotificationListener()

	return client, nil
}

func (c *Client) Close() error {
	c.protocolClient.StopNotificationListener()
	return c.conn.Close()
}

func (c *Client) Run() error {
	fmt.Println("Connected to server")
	fmt.Println("Type 'help' for available commands")

	scanner := bufio.NewScanner(os.Stdin)

	for {
		fmt.Print("> ")
		if !scanner.Scan() {
			break
		}

		line := strings.TrimSpace(scanner.Text())
		if line == "" {
			continue
		}

		parts := strings.Fields(line)
		if len(parts) == 0 {
			continue
		}

		cmd := parts[0]

		handler, exists := c.commandRegistry.Get(cmd)
		if !exists {
			fmt.Printf("Unknown command: %s. Type 'help' for available commands\n", cmd)
			continue
		}

		ctx := &commands.Context{
			ProtocolClient: c.protocolClient,
			Authenticated:  &c.authenticated,
		}

		err := handler(ctx, parts)
		if err != nil {
			fmt.Printf("Error: %v\n", err)
		}
	}

	return nil
}

