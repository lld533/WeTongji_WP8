using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace WeTongji.Api.Request
{
    public class UserUpdateAvatarRequest<T> : WTUploadFileRequest<T> where T : WeTongji.Api.Response.UserGetResponse
    {
        #region [Constructor]

        public UserUpdateAvatarRequest()
        {
        }

        #endregion

        #region [Property]

        public Stream JpegPhotoStream { get; set; }

        #endregion

        #region [Overridden]

        public override IDictionary<String, String> GetParameters()
        {
            return base.dict;
        }

        public override String GetApiName()
        {
            return "User.Update.Avatar";
        }

        public override void Validate()
        {
            if (JpegPhotoStream == null)
            {
                throw new ArgumentNullException("JpegPhotoStream");
            }
            else if (!JpegPhotoStream.CanRead)
            {
                throw new NotSupportedException("JpegPhotoStream should be able to read.");
            }
            else if (!JpegPhotoStream.CanSeek)
            {
                throw new NotSupportedException("JpegPhotoStream should be able to seek.");
            }
        }

        public override IEnumerable<MyToolkit.Networking.HttpPostFile> GetFiles() 
        { 
            return new MyToolkit.Networking.HttpPostFile[]
                {
                    new MyToolkit.Networking.HttpPostFile("Image", "avatar.jpg", JpegPhotoStream, false)
                }; 
        }

        #endregion
    }
}
