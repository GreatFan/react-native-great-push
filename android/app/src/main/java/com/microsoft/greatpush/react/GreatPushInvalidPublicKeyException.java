package com.microsoft.greatpush.react;

class GreatPushInvalidPublicKeyException extends RuntimeException {

    public GreatPushInvalidPublicKeyException(String message, Throwable cause) {
        super(message, cause);
    }

    public GreatPushInvalidPublicKeyException(String message) {
        super(message);
    }
}