package commands

import (
	"fmt"
	"os"
	"path/filepath"

	"GoClient/internal/config"
	"GoClient/internal/protocol"
)

type Context struct {
	ProtocolClient *protocol.Client
	Authenticated  *bool
}

type HandlerFunc func(ctx *Context, args []string) error

func HandleHelp(ctx *Context, args []string) error {
	fmt.Println("Available commands:")
	fmt.Println("  auth <username> <password>  - Authenticate with server")
	fmt.Println("  list                       - List files on server")
	fmt.Println("  download <filename>         - Download a file from server")
	fmt.Println("  upload <filename>          - Upload a file to server")
	fmt.Println("  ping                        - Ping server")
	fmt.Println("  help                        - Show this help")
	fmt.Println("  quit/exit                   - Exit client")
	return nil
}

func HandleAuth(ctx *Context, args []string) error {
	if len(args) != 3 {
		return fmt.Errorf("usage: auth <username> <password>")
	}

	err := ctx.ProtocolClient.Auth(args[1], args[2])
	if err != nil {
		return fmt.Errorf("auth failed: %v", err)
	}

	*ctx.Authenticated = true
	fmt.Println("Authentication successful")
	return nil
}

func HandleList(ctx *Context, args []string) error {
	if !*ctx.Authenticated {
		return fmt.Errorf("you must authenticate first. Use 'auth <username> <password>'")
	}

	files, err := ctx.ProtocolClient.ListFiles()
	if err != nil {
		return err
	}

	fmt.Println("Files on server:")
	for _, f := range files {
		fmt.Printf("  - %s\n", f)
	}
	return nil
}

func HandleDownload(ctx *Context, args []string) error {
	if !*ctx.Authenticated {
		return fmt.Errorf("you must authenticate first. Use 'auth <username> <password>'")
	}

	err := os.MkdirAll(config.ClientFilesDir, 0755)
	if err != nil {
		return fmt.Errorf("failed to create client directory: %v", err)
	}

	serverFiles, err := ctx.ProtocolClient.ListFiles()
	if err != nil {
		return err
	}

	localFiles, err := getLocalFiles()
	if err != nil {
		return err
	}

	localFileMap := make(map[string]bool)
	for _, f := range localFiles {
		localFileMap[f] = true
	}

	downloaded := 0
	for _, filename := range serverFiles {
		if !localFileMap[filename] {
			fmt.Printf("  Downloading: %s\n", filename)
			savePath := filepath.Join(config.ClientFilesDir, filename)
			err := ctx.ProtocolClient.DownloadFile(filename, savePath)
			if err != nil {
				return fmt.Errorf("failed to download %s: %v", filename, err)
			}
			downloaded++
		}
	}

	if downloaded == 0 {
		fmt.Println("All files are up to date")
	} else {
		fmt.Printf("Downloaded %d file(s)\n", downloaded)
	}
	return nil
}

func HandleUpload(ctx *Context, args []string) error {
	if !*ctx.Authenticated {
		return fmt.Errorf("you must authenticate first. Use 'auth <username> <password>'")
	}

	err := os.MkdirAll(config.ClientFilesDir, 0755)
	if err != nil {
		return fmt.Errorf("failed to create client directory: %v", err)
	}

	serverFiles, err := ctx.ProtocolClient.ListFiles()
	if err != nil {
		return err
	}

	localFiles, err := getLocalFiles()
	if err != nil {
		return err
	}

	serverFileMap := make(map[string]bool)
	for _, f := range serverFiles {
		serverFileMap[f] = true
	}

	uploaded := 0
	for _, filename := range localFiles {
		if !serverFileMap[filename] {
			fmt.Printf("  Uploading: %s\n", filename)
			fullPath := filepath.Join(config.ClientFilesDir, filename)
			err := ctx.ProtocolClient.UploadFile(fullPath)
			if err != nil {
				return fmt.Errorf("failed to upload %s: %v", filename, err)
			}
			uploaded++
		}
	}

	if uploaded == 0 {
		fmt.Println("All files are up to date")
	} else {
		fmt.Printf("Uploaded %d file(s)\n", uploaded)
	}
	return nil
}

func getLocalFiles() ([]string, error) {
	entries, err := os.ReadDir(config.ClientFilesDir)
	if err != nil {
		if os.IsNotExist(err) {
			return []string{}, nil
		}
		return nil, err
	}

	var files []string
	for _, entry := range entries {
		if !entry.IsDir() {
			files = append(files, entry.Name())
		}
	}
	return files, nil
}

func HandlePing(ctx *Context, args []string) error {
	if !*ctx.Authenticated {
		return fmt.Errorf("you must authenticate first. Use 'auth <username> <password>'")
	}

	ok, err := ctx.ProtocolClient.Ping()
	if err != nil {
		return err
	}

	fmt.Printf("Ping: %v\n", ok)
	return nil
}

func HandleQuit(ctx *Context, args []string) error {
	fmt.Println("Goodbye!")
	os.Exit(0)
	return nil
}

