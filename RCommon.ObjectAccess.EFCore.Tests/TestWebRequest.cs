using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RCommon.ObjectAccess.EFCore.Tests
{
    // <summary>A web request creator for unit testing.</summary> 
    public class TestWebRequestCreate : IWebRequestCreate
    {
        static WebRequest nextRequest;
        static object lockObject = new object();
        static public WebRequest NextRequest
        {
            get { return nextRequest; }
            set
            {
                lock (lockObject) { nextRequest = value; }
            }
        }

        /// <summary>See <see cref="IWebRequestCreate.Create"/>.</summary>    
        public WebRequest Create(Uri uri) { return nextRequest; }

        /// <summary>Utility method for creating a TestWebRequest and setting it to be the next WebRequest to use.</summary> 
        /// <param name="response">The response the TestWebRequest will return.</param>   
        public static TestWebRequest CreateTestRequest(string response) 
        { 
            TestWebRequest request = new TestWebRequest(response); 
            NextRequest = request; 
            return request; 
        }
    }


    public class TestWebRequest : WebRequest
    {
        MemoryStream requestStream = new MemoryStream(); MemoryStream responseStream; 
        
        public override string Method { get; set; }
        public override string ContentType { get; set; }
        public override long ContentLength { get; set; }

        /// <summary>Initializes a new instance of <see cref="TestWebRequest"/>    
        /// with the response to return.</summary>    
        public TestWebRequest(string response) 
        { 
            responseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response)); 
        }

        /// <summary>Returns the request contents as a string.</summary>    
        public string ContentAsString() 
        { 
            return System.Text.Encoding.UTF8.GetString(requestStream.ToArray()); 
        }


        /// <summary>See <see cref="WebRequest.GetRequestStream"/>.</summary>    
        public override Stream GetRequestStream() 
        { 
            return requestStream; 
        }

        /// <summary>See <see cref="WebRequest.GetResponse"/>.</summary>   
        public override WebResponse GetResponse() 
        { 
            return new TestWebReponse(responseStream); 
        }
    }

    public class TestWebReponse : WebResponse
    {
        Stream responseStream;

        /// <summary>Initializes a new instance of <see cref="TestWebReponse"/>    
        /// with the response stream to return.</summary>    
        public TestWebReponse(Stream responseStream) 
        { 
            this.responseStream = responseStream; 
        }


        /// <summary>See <see cref="WebResponse.GetResponseStream"/>.</summary>    
        public override Stream GetResponseStream() 
        { 
            return responseStream; 
        }
    }
}

