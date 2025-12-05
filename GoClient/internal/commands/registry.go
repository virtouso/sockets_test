package commands

type Registry struct {
	handlers map[string]HandlerFunc
}

func NewRegistry() *Registry {
	registry := &Registry{
		handlers: make(map[string]HandlerFunc),
	}

	registry.Register("help", HandleHelp)
	registry.Register("auth", HandleAuth)
	registry.Register("list", HandleList)
	registry.Register("download", HandleDownload)
	registry.Register("upload", HandleUpload)
	registry.Register("ping", HandlePing)
	registry.Register("quit", HandleQuit)
	registry.Register("exit", HandleQuit)

	return registry
}

func (r *Registry) Register(cmd string, handler HandlerFunc) {
	r.handlers[cmd] = handler
}

func (r *Registry) Get(cmd string) (HandlerFunc, bool) {
	handler, exists := r.handlers[cmd]
	return handler, exists
}

