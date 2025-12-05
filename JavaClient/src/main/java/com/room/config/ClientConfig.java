package com.room.config;

import java.io.File;

public class ClientConfig {
    public static String getClientFilesDir() {
        try {
            String codeSourcePath = ClientConfig.class.getProtectionDomain()
                    .getCodeSource()
                    .getLocation()
                    .toURI()
                    .getPath();
            
            File codeSourceFile = new File(codeSourcePath);
            File parentDir = codeSourceFile.getParentFile();
            
            // When running from IDE: codeSource is target/classes, parent is target
            // When running from JAR: codeSource is the JAR file, parent is the directory containing the JAR
            // Put client_files next to where the app files are (target directory or JAR directory)
            if (parentDir != null) {
                return new File(parentDir, "client_files").getAbsolutePath();
            }
            
            return new File("client_files").getAbsolutePath();
        } catch (Exception e) {
            return new File("client_files").getAbsolutePath();
        }
    }
}

