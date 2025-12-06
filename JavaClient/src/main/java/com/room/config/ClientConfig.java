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
            

            if (parentDir != null) {
                return new File(parentDir, "client_files").getAbsolutePath();
            }
            
            return new File("client_files").getAbsolutePath();
        } catch (Exception e) {
            return new File("client_files").getAbsolutePath();
        }
    }
}

