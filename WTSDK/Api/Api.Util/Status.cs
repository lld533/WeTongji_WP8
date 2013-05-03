using System;

namespace WeTongji.Api.Util
{
    public enum Status:int
    {
        /// <summary>
        /// internal service error
        /// </summary>
        InternalServiceError = 999,
        
        /// <summary>
        /// success
        /// </summary>
        Success = 0,

        /// <summary>
        /// checksum failed
        /// </summary>
        ChecksumError = 1,

        /// <summary>
        /// invalid method
        /// </summary>
        InvalidMethod = 2,

        /// <summary>
        /// invalid parameter
        /// </summary>
        InvalidParameter = 3,

        /// <summary>
        /// lack must system parameter
        /// </summary>
        LackMustSystemParameter = 4,

        /// <summary>
        /// no authentication, please login
        /// </summary>
        NoAuth = 5,

        /// <summary>
        /// login time out, login again
        /// </summary>
        LoginTimeOut = 6,

        /// <summary>
        /// lack must parameter
        /// </summary>
        LackMustParameter = 7,

        /// <summary>
        /// User is already registered.
        /// </summary>
        AlreadyRegistered = 8,

        /// <summary>
        /// Id dis-matches name.
        /// </summary>
        IdNameDismatch = 9,

        /// <summary>
        /// invalid password
        /// </summary>
        InvalidPassword = 10,

        /// <summary>
        /// Account is not activated
        /// </summary>
        NotActivatedAccount = 11,

        /// <summary>
        /// No such account
        /// </summary>
        NoAccount = 12,

        /// <summary>
        /// Account dis-matches password
        /// </summary>
        AccountPasswordDismatch = 13,

        /// <summary>
        /// Account is not registered.
        /// </summary>
        NotRegistered = 14
    }
}
