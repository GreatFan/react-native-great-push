package com.microsoft.greatpush.react;

public enum GreatPushUpdateState {
    RUNNING(0),
    PENDING(1),
    LATEST(2);

    private final int value;
    GreatPushUpdateState(int value) {
        this.value = value;
    }
    public int getValue() {
        return this.value;
    }
}