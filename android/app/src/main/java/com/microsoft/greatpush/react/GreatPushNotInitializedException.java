package com.microsoft.greatpush.react;

public final class GreatPushNotInitializedException extends RuntimeException {

    public GreatPushNotInitializedException(String message, Throwable cause) {
        super(message, cause);
    }

    public GreatPushNotInitializedException(String message) {
        super(message);
    }
}