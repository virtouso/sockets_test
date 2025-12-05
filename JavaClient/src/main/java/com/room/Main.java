package com.room;

import com.room.client.Client;

import java.net.Socket;
import java.util.Scanner;

public class Main {
    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);

        System.out.print("Enter server IP [127.0.0.1]: ");
        String serverAddress = scanner.nextLine().trim();
        if (serverAddress.isEmpty()) {
            serverAddress = "127.0.0.1";
        }

        System.out.print("Enter server port [8080]: ");
        String portInput = scanner.nextLine().trim();
        int serverPort = 8080;
        if (!portInput.isEmpty()) {
            try {
                serverPort = Integer.parseInt(portInput);
            } catch (NumberFormatException e) {
                System.err.println("Invalid port number, using default 8080");
            }
        }

        try (Socket socket = new Socket(serverAddress, serverPort)) {
            Client client = new Client(socket.getInputStream(), socket.getOutputStream());
            client.run();
        } catch (Exception e) {
            System.err.println("Connection error: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
