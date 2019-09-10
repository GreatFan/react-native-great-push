#import "GreatPush.h"

@implementation GreatPushErrorUtils

static NSString *const GreatPushErrorDomain = @"GreatPushError";
static const int GreatPushErrorCode = -1;

+ (NSError *)errorWithMessage:(NSString *)errorMessage
{
    return [NSError errorWithDomain:GreatPushErrorDomain
                               code:GreatPushErrorCode
                           userInfo:@{ NSLocalizedDescriptionKey: NSLocalizedString(errorMessage, nil) }];
}

+ (BOOL)isGreatPushError:(NSError *)err
{
    return err != nil && [GreatPushErrorDomain isEqualToString:err.domain];
}

@end