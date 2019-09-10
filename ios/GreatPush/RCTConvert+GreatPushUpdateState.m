#import "GreatPush.h"

#if __has_include(<React/RCTConvert.h>)
#import <React/RCTConvert.h>
#else
#import "RCTConvert.h"
#endif

// Extending the RCTConvert class allows the React Native
// bridge to handle args of type "GreatPushUpdateState"
@implementation RCTConvert (GreatPushUpdateState)

RCT_ENUM_CONVERTER(GreatPushUpdateState, (@{ @"greatPushUpdateStateRunning": @(GreatPushUpdateStateRunning),
                                            @"greatPushUpdateStatePending": @(GreatPushUpdateStatePending),
                                            @"greatPushUpdateStateLatest": @(GreatPushUpdateStateLatest)
                                          }),
                   GreatPushUpdateStateRunning, // Default enum value
                   integerValue)

@end
