package main

import (
	"bufio"
	"fmt"
	"os"
	"strings"

	"GoClient/internal/client"
)

func main() {
	scanner := bufio.NewScanner(os.Stdin)

	fmt.Print("Enter server IP [127.0.0.1]: ")
	scanner.Scan()
	ip := strings.TrimSpace(scanner.Text())
	if ip == "" {
		ip = "127.0.0.1"
	}

	fmt.Print("Enter server port [8080]: ")
	scanner.Scan()
	port := strings.TrimSpace(scanner.Text())
	if port == "" {
		port = "8080"
	}

	serverAddr := ip + ":" + port

	cli, err := client.NewClient(serverAddr)
	if err != nil {
		fmt.Printf("Failed to connect: %v\n", err)
		os.Exit(1)
	}
	defer cli.Close()

	if err := cli.Run(); err != nil {
		fmt.Printf("Error: %v\n", err)
		os.Exit(1)
	}
}

