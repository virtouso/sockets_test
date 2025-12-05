package client

import (
	"bufio"
	"fmt"
	"net"
	"os"
	"strings"

	"GoClient/internal/commands"
	"GoClient/internal/protocol"
)

type Client struct {
	conn           net.Conn
	protocolClient *protocol.Client
	commandRegistry *commands.Registry
	authenticated  bool
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
	protocolClient.SetOnFileUploadedCallback(func(fileName string) {
		fmt.Printf("\n[Notification] File uploaded: %s\n", fileName)
		fmt.Println("[Auto-download] Downloading missing files...")
		
		ctx := &commands.Context{
			ProtocolClient: protocolClient,
			Authenticated:  &client.authenticated,
		}
		
		// Trigger download handler
		downloadHandler := commands.HandleDownload
		err := downloadHandler(ctx, []string{"download"})
		if err != nil {
			fmt.Printf("Error during auto-download: %v\n", err)
		}
		fmt.Print("> ") // Restore prompt
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

