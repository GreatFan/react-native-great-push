package com.microsoft.greatpush.react;

import java.net.MalformedURLException;

public class GreatPushMalformedDataException extends RuntimeException {
    public GreatPushMalformedDataException(String path, Throwable cause) {
        super("Unable to parse contents of " + path + ", the file may be corrupted.", cause);
    }
    public GreatPushMalformedDataException(String url, MalformedURLException cause) {
        super("The package has an invalid downloadUrl: " + url, cause);
    }
}