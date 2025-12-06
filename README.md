# sockets_test

## better approach:
like a modern app using combination of grpc and http to download and upload files
is more robust and less error prone. 
grpc simply supports cross language communication
notify clients with grpc and list files and use http to download or upload files

but as the assigment asked to use no external tools i used pure sockets.


## things i supported:
1) Tests
2) Admin Panel
3) Argument based commandline clients
4) .net core server. Go client. Java client
5) Dockerfile and Docker Compose

* General Approach:

used combination of command size and content to read messages. for handling message first read single byte to know the command
then get command size and finally getting the content of the command.
there are other approaches, but basically it works fine for such scenarios.

instead of using switch case that violates open-close principle, i use dictionary of command and handler or delegate. 

* Tests

I mostly considered doing integration tests instead of unit tests.
writing unit tests requires putting everything behind interfaces 
to be able to mock dependencies and handlers in my app highly depend on streams.
doing integration is simpler and tests the system as a whole. 

* Admin Panel

web apps by default dont support pure sockets. there was a simple way to solve the problems. using a shared resource like redis
to manage data.
socket app writes on redis and asp core http server reads to respond to the web client. 
i used blazor wasm to make simple single page app that shows server stats. 

* Docker
server apps support docker
use docker compose to simply run all of them at once and also redis. 
