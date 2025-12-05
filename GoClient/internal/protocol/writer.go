package protocol

import (
	"encoding/binary"
	"net"
)

func WriteInt(conn net.Conn, v int) {
	buf := make([]byte, 4)
	binary.LittleEndian.PutUint32(buf, uint32(v))
	conn.Write(buf)
}

func WriteInt64(conn net.Conn, v int64) {
	buf := make([]byte, 8)
	binary.LittleEndian.PutUint64(buf, uint64(v))
	conn.Write(buf)
}

