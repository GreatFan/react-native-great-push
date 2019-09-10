package com.microsoft.greatpush.react;

class GreatPushUnknownException extends RuntimeException {

    public GreatPushUnknownException(String message, Throwable cause) {
        super(message, cause);
    }

    public GreatPushUnknownException(String message) {
        super(message);
    }
}