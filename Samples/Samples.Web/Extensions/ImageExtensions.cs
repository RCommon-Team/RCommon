using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RCommon.Util;

namespace Samples.Web.Extensions
{
    public static class ImageExtensions
    {

        public static HtmlString DisplayImage(this IHtmlHelper html, byte[] image)
        {
            // TODO: All this seems very expensive. Need to see if there is a better way to process images.
            var format = ImageHelper.GetImageFormat(image);

            string img = string.Empty;

            // We only want to support Jpeg, and Png
            switch (format)
            {
                case ImageHelper.ImageFormat.Jpeg:
                    img = String.Format("data:image/jpg;base64,{0}", Convert.ToBase64String(image));
                    break;
                case ImageHelper.ImageFormat.Png:
                    img = String.Format("data:image/png;base64,{0}", Convert.ToBase64String(image));
                    break;
                default:
                    break;
            }
            return new HtmlString("<img src='" + img + "' />");
        }
    }
}
