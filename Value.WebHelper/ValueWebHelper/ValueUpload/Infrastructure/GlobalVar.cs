using System;

namespace ValueWebHelper.ValueUpload.Infrastructure
{
    public class GlobalVar
    {
        /// <summary>
        ///  千字节
        /// </summary>
        public const Int32 Kilobyte = 1024;

        /// <summary>
        ///  换行符
        /// </summary>
        public static String newline = Environment.NewLine;

        /// <summary>
        ///  获得Http报文中分割的边界字符串
        /// </summary>
        public const String BoundaryPattern = @"\s+boundary=(?("")""(?<boundary>.*)""|(?<boundary>.*))";

        /// <summary>
        ///  Content-Disposition
        ///  Content-Type
        /// </summary>
        public const String MIMEHeadPattern = @"Content-Disposition:\s*form-data;\s+name=""(?<tagname>\w*)"";\s+" +
                @"filename=""(?<filename>.*)""\r\nContent-Type:\s*(?<type>.*)$";

        public const String MIMEFormPattern = @"Content-Disposition:\s*form-data;\s+name=""(?<tagname>\w*)""$";
    }
}
