/*
  >>>------ Copyright (c) 2012 zformular ----> 
 |                                            |
 |            Author: zformular               |
 |        E-mail: zformular@163.com           |
 |             Date: 10.7.2012                |
 |                                            |
 ╰==========================================╯
 
*/

using System;
using System.Text;
using System.Web;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using ValueWebHelper.ValueUpload.UploadEvents;

namespace ValueWebHelper.ValueUpload.Infrastructure
{
    public class UploadBase
    {
        protected HttpWorkerRequest httpWorkerRequest;
        protected HttpContextBase httpContextBase;
        protected Encoding encoding;

        protected event UpdateDelegate OnUpdate;
        protected UpdateEventArgs updateEventArgs = new UpdateEventArgs();


        private SaveInfo saveInfo = null;
        private UploadInfo uploadInfo = null;
        private Int32 fileIndex = 0;

        protected UploadInfo Save(String filePath)
        {
            try
            {
                saveInfo = new SaveInfo();
                saveInfo.Type = "Save";
                saveInfo.FilePath = filePath;

                uploadInfo = new UploadInfo();
                uploadProcess();
                uploadInfo.Success = true;
            }
            catch (Exception ex)
            {
                uploadInfo.Success = false;
                uploadInfo.Exception = ex;
            }
            return uploadInfo;
        }

        protected UploadInfo SaveAs(String filePath, params String[] fileName)
        {
            try
            {
                if (fileName == null || fileName.Length == 0)
                {
                    return Save(filePath);
                }

                saveInfo = new SaveInfo();
                saveInfo.Type = "SaveAs";
                saveInfo.FilePath = filePath;
                saveInfo.FileNames = fileName;
                uploadInfo = new UploadInfo();
                uploadProcess();
                uploadInfo.Success = true;
            }
            catch (Exception ex)
            {
                uploadInfo.Success = false;
                uploadInfo.Exception = ex;
            }
            return uploadInfo;
        }

        /// <summary>
        ///  循环处理读取的数据包
        /// </summary>
        /// <param name="processType"></param>
        private void uploadProcess()
        {
            String contentType = httpWorkerRequest.GetKnownRequestHeader(HttpWorkerRequest.HeaderContentType);
            String boundary = "--" + Regex.Match(contentType, GlobalVar.BoundaryPattern).Groups["boundary"].Value;
            this.setRecogizeBytes(boundary);

            Int32 preLoaded = httpWorkerRequest.GetPreloadedEntityBodyLength();
            Int32 totalLength = httpWorkerRequest.GetTotalEntityBodyLength();

            Byte[] buffer = new Byte[preLoaded];
            buffer = httpWorkerRequest.GetPreloadedEntityBody();
            analyseMIME(buffer);

            if (httpWorkerRequest.IsEntireEntityBodyIsPreloaded())
            {
                analyseRemain();
                return;
            }
            // 该类负责欺骗ASP.NET 让其认为没有文件上传就不会尝试缓存文件,不会报文件太大的错
            StaticWorkerRequest staticWorkerRequest = new StaticWorkerRequest(httpWorkerRequest, httpWorkerRequest.GetPreloadedEntityBody());
            FieldInfo field = HttpContext.Current.Request.GetType().GetField("_wr", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(HttpContext.Current.Request, staticWorkerRequest);

            Int32 loaded = 0;
            Int32 received = preLoaded;
            // 循环直到文件读取完毕为止
            while (totalLength > received && httpWorkerRequest.IsClientConnected())
            {
                buffer = new Byte[GlobalVar.Kilobyte];
                loaded = httpWorkerRequest.ReadEntityBody(buffer, buffer.Length);
                buffer = cutBuffer(buffer, loaded);
                analyseMIME(buffer);
                received += loaded;

                if (OnUpdate != null)
                    OnUpdate(new UpdateEventArgs
                    {
                        Progress = ((Double)received / (Double)totalLength).ToString()
                    });
            }

            analyseRemain();
        }

        // 换行比特
        private Byte[] newlineBytes;
        // 边界比特
        private Byte[] boundaryBytes;
        // 边界行比特
        private Byte[] boundaryNexLineBytes;
        // 行边界比特
        private Byte[] boundaryPreLineBytes;
        // 双换行比特
        private Byte[] doubleNewLine;
        // 设置辨别字符的比特形式
        private void setRecogizeBytes(String boundary)
        {
            this.boundaryBytes = encoding.GetBytes(boundary);
            this.newlineBytes = encoding.GetBytes(GlobalVar.newline);
            this.boundaryNexLineBytes = encoding.GetBytes(String.Concat(boundary, GlobalVar.newline));
            this.boundaryPreLineBytes = encoding.GetBytes(String.Concat(GlobalVar.newline, boundary));
            this.doubleNewLine = encoding.GetBytes(String.Concat(GlobalVar.newline, GlobalVar.newline));
        }

        private Byte[] remainBytes = null;
        private FileStream fileStream = null;
        // 分析MIME创建文件并写入内容
        private void analyseMIME(Byte[] buffer)
        {
            buffer = combineRemainBytes(buffer);
            var bondLineIndex = getIndexOf(buffer, this.boundaryNexLineBytes);
            if (bondLineIndex == -1)
            {
                buffer = cutRightBytes(buffer);
                fileStream.Write(buffer, 0, buffer.Length);
                fileStream.Flush();
            }
            else
            {
                // 如果分割线大于0, 那么先把文件学完,再分析
                if (bondLineIndex > 0)
                {
                    var newBuffer = cutBuffer(buffer, bondLineIndex - this.newlineBytes.Length);
                    fileStream.Write(newBuffer, 0, newBuffer.Length);
                    fileStream.Flush();
                    fileStream.Close();
                    fileStream.Dispose();

                    // 分割线开始的数据
                    buffer = cutBuffer(buffer, bondLineIndex, buffer.Length - bondLineIndex);
                    bondLineIndex = 0;
                }

                // 正好获得 mime头 则解析并创建文件
                var cutLineIndex = getIndexOf(buffer, this.doubleNewLine);
                if (cutLineIndex != -1)
                {
                    var startIndex = this.boundaryNexLineBytes.Length;
                    var fileBuffer = cutBuffer(buffer, startIndex, cutLineIndex - startIndex);
                    var text = encoding.GetString(fileBuffer);
                    var match = Regex.Match(text, GlobalVar.MIMEHeadPattern);
                    if (match.Success)
                    {
                        var realStart = cutLineIndex + this.doubleNewLine.Length;
                        // 剪切到文件内容部分
                        buffer = cutBuffer(buffer, realStart, buffer.Length - realStart);

                        var groups = match.Groups;
                        if (groups["filename"].Value != String.Empty)
                        {
                            var fileInfo = new FileInfo
                            {
                                FileTagName = groups["tagname"].Value,
                                FileName = groups["filename"].Value,
                                SaveName = groups["filename"].Value
                            };
                            if (saveInfo.Type == "SaveAs")
                                fileInfo.SaveName = saveInfo.FileNames[fileIndex];
                            this.uploadInfo.Files.Add(fileInfo);
                            this.fileStream = new FileStream(Path.Combine(saveInfo.FilePath, fileInfo.SaveName), FileMode.CreateNew);
                            fileIndex++;
                        }
                        else// 如果文件创建失败(目前只发生有fileTag当时没有添加文件),那么自动减去文件内容(即一个换行符)
                            buffer = cutBuffer(buffer, this.newlineBytes.Length, buffer.Length - this.newlineBytes.Length);

                        analyseMIME(buffer);
                    }
                    else// 如果没有匹配文件头那个说明文件集合已经上传完毕, 只剩表单的内容了
                        increaseRemainBytes(buffer);
                }
                else// 没有mime头的话先存下来
                    increaseRemainBytes(buffer);
            }
        }

        private void analyseRemain()
        {
            if (this.remainBytes != null)
            {
                var bondPreLineIndex = getIndexOf(this.remainBytes, this.boundaryPreLineBytes);
                if (bondPreLineIndex != -1)
                {
                    var formBytes = cutBuffer(this.remainBytes, bondPreLineIndex);
                    var cutLineIndex = getIndexOf(formBytes, this.doubleNewLine);
                    if (cutLineIndex != -1)
                    {
                        var text = encoding.GetString(formBytes, 0, cutLineIndex);
                        var match = Regex.Match(text, GlobalVar.MIMEFormPattern);
                        if (match.Success)
                        {
                            var realyStart = cutLineIndex + this.doubleNewLine.Length;
                            var value = encoding.GetString(formBytes, realyStart, formBytes.Length - realyStart);
                            this.uploadInfo.Form.Add(match.Groups["tagname"].Value, value);
                        }

                        var realStart = bondPreLineIndex + this.newlineBytes.Length;
                        this.remainBytes = cutBuffer(this.remainBytes, realStart, this.remainBytes.Length - realStart);
                        analyseRemain();
                    }
                }
            }
        }

        #region Byte[] OperSet

        private Byte[] cutRightBytes(Byte[] buffer)
        {
            if (buffer.Length > 100)
            {
                var newBuffer = new Byte[buffer.Length - 100];
                Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length - 100);
                this.remainBytes = new Byte[100];
                Buffer.BlockCopy(buffer, buffer.Length - 100, this.remainBytes, 0, 100);
                return newBuffer;
            }
            return buffer;
        }

        private Byte[] combineRemainBytes(Byte[] buffer)
        {
            if (remainBytes == null) return buffer;

            Int32 len = remainBytes.Length;
            Int32 len2 = buffer.Length;
            Byte[] newBuffer = new Byte[len + len2];
            Buffer.BlockCopy(remainBytes, 0, newBuffer, 0, len);
            Buffer.BlockCopy(buffer, 0, newBuffer, len, len2);

            this.remainBytes = null;
            return newBuffer;
        }

        private void increaseRemainBytes(Byte[] buffer)
        {
            if (this.remainBytes == null || this.remainBytes.Length == 0)
            {
                this.remainBytes = buffer;
                return;
            }

            var len = this.remainBytes.Length;
            var len2 = buffer.Length;

            var newBuffer = new Byte[len + len2];
            Buffer.BlockCopy(this.remainBytes, 0, newBuffer, 0, len);
            Buffer.BlockCopy(buffer, 0, newBuffer, len, len2);
            this.remainBytes = newBuffer;
        }

        private Int32 getIndexOf(Byte[] array, Byte[] value)
        {
            var startMatch = Array.IndexOf(array, value[0]);
            if (startMatch == -1) return -1;

            Int32 index = 0;
            while ((startMatch + index) < array.Length)
            {
                if (array[startMatch + index] == value[index])
                {
                    index++;
                    if (index == value.Length)
                        return startMatch;
                }
                else
                {
                    startMatch = Array.IndexOf(array, value[0], startMatch + index);
                    if (startMatch != -1)
                        index = 0;
                    else
                        return -1;
                }
            }
            return -1;
        }

        private Byte[] cutBuffer(Byte[] buffer, Int32 startX, Int32 length)
        {
            if (length < 0) return buffer;
            Byte[] newBuffer = new Byte[length];
            Buffer.BlockCopy(buffer, startX, newBuffer, 0, length);
            return newBuffer;
        }

        private Byte[] cutBuffer(Byte[] buffer, Int32 length)
        {
            if (length < 0) return buffer;
            Byte[] newBuffer = new Byte[length];
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, length);
            return newBuffer;
        }

        #endregion
    }
}
