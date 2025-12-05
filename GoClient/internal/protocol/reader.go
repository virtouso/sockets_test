package protocol

import (
	"encoding/binary"
	"io"
	"net"
)

func ReadInt(conn net.Conn) (int, error) {
	buf := make([]byte, 4)
	_, err := io.ReadFull(conn, buf)
	if err != nil {
		return 0, err
	}
	return int(binary.LittleEndian.Uint32(buf)), nil
}

func ReadInt64(conn net.Conn) (int64, error) {
	buf := make([]byte, 8)
	_, err := io.ReadFull(conn, buf)
	if err != nil {
		return 0, err
	}
	return int64(binary.LittleEndian.Uint64(buf)), nil
}

func ReadBytes(conn net.Conn, length int) ([]byte, error) {
	buf := make([]byte, length)
	_, err := io.ReadFull(conn, buf)
	return buf, err
}

