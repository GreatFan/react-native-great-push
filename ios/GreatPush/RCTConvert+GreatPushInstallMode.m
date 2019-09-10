#import "GreatPush.h"

#if __has_include(<React/RCTConvert.h>)
#import <React/RCTConvert.h>
#else
#import "RCTConvert.h"
#endif

// Extending the RCTConvert class allows the React Native
// bridge to handle args of type "GreatPushInstallMode"
@implementation RCTConvert (GreatPushInstallMode)

RCT_ENUM_CONVERTER(GreatPushInstallMode, (@{ @"greatPushInstallModeImmediate": @(GreatPushInstallModeImmediate),
                                            @"greatPushInstallModeOnNextRestart": @(GreatPushInstallModeOnNextRestart),
                                            @"greatPushInstallModeOnNextResume": @(GreatPushInstallModeOnNextResume),
                                            @"greatPushInstallModeOnNextSuspend": @(GreatPushInstallModeOnNextSuspend) }),
                   GreatPushInstallModeImmediate, // Default enum value
                   integerValue)

@end
