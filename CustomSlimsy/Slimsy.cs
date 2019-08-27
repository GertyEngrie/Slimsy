// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Slimsy.cs" company="Our.Umbraco">
//   2017
// </copyright>
// <summary>
//   Defines the Slimsy type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Runtime.Caching;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Serialization.Formatters;


namespace CustomSlimsy
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Linq;
    using System.Text;
    using System.Web;
    using System.Web.Mvc;

    using HtmlAgilityPack;
    using Newtonsoft.Json;

    using Umbraco.Core;
    using Umbraco.Core.Cache;
    using Umbraco.Web;
    using Umbraco.Web.Models;
    using Umbraco.Web.PropertyEditors.ValueConverters;
    using Umbraco.Core.PropertyEditors;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Core.PropertyEditors.ValueConverters;
    using Umbraco.Web.Macros;
    using Constants = Umbraco.Core.Constants;
    using Current = Umbraco.Web.Composing.Current;

    [System.Runtime.InteropServices.Guid("38B09B03-3029-45E8-BC21-21C8CC8D4278")]
    public static class Slimsy
    {

        #region SrcSet


        // focalpoint picture, dynamic, ?options  (veronderstellend in screensize stijgend, ratio constant tot nieuwe entry)
        // overrides / manueel
        // only attributes

        // focalpoint picture, width, height,  ?options(320w dimension, ratio constant)
        public static IHtmlString GetSrcSetUrls(this UrlHelper urlHelper, IPublishedContent publishedContent, int width, int height, Dictionary<string, string> options = null, ImageCropMode cropmode = ImageCropMode.Crop)
        {
            var w = WidthStep();
            var q = publishedContent.HasProperty("quality") &&
                    publishedContent.Value<int>("quality") > 0
                ? publishedContent.Value<int>("quality")
                : DefaultQuality();

            var outputStringBuilder = new StringBuilder();

            decimal heightRatio;
            if (height == 0 || width == 0)
            {
                heightRatio = (decimal) publishedContent.Value<int>("umbracoHeight") / publishedContent.Value<int>("umbracoWidth");
            }
            else
            {
                heightRatio = (decimal) height / width;
            }
            // niet croppen indien value aanstaat en crop gevraagd wordt
            var mode = publishedContent.HasProperty("doNotCrop") &&
                       publishedContent.Value<bool>("doNotCrop") && cropmode == ImageCropMode.Crop
                ? ImageCropMode.Max
                : cropmode;

            while (w <= MaxWidth(publishedContent))
            {
                var h = (int)Math.Round(w * heightRatio);
                var cropString = urlHelper.GetCropUrl(publishedContent, w, h, "umbracoFile", quality: q, preferFocalPoint: true,
                    furtherOptions: Format(publishedContent, options), htmlEncode: false, imageCropMode: mode).ToString();

                outputStringBuilder.Append($"{cropString} {w}w,");
                w += WidthStep();
            }

            // remove the last comma
            var outputString = outputStringBuilder.ToString().Substring(0, outputStringBuilder.Length - 1);

            return new HtmlString(HttpUtility.HtmlEncode(outputString));
        }

        public static IHtmlString GetSrcSetUrls(this UrlHelper urlHelper, IPublishedContent publishedContent, Dictionary<int,int[]> imageSizes , Dictionary<string, string> options = null, ImageCropMode cropmode = ImageCropMode.Crop)
        {
            var w = WidthStep();
            var q = publishedContent.HasProperty("quality") &&
                    publishedContent.Value<int>("quality") > 0
                ? publishedContent.Value<int>("quality")
                : DefaultQuality();

            var outputStringBuilder = new StringBuilder();

            // niet croppen indien value aanstaat en crop gevraagd wordt
            var mode = publishedContent.HasProperty("doNotCrop") &&
                       publishedContent.Value<bool>("doNotCrop") && cropmode == ImageCropMode.Crop
                ? ImageCropMode.Max
                : cropmode;

            var heightRatio = (decimal) publishedContent.Value<int>("umbracoHeight") /publishedContent.Value<int>("umbracoWidth");
            while (w <= MaxWidth(publishedContent))
            {
                if (imageSizes.ContainsKey(w))
                {
                    heightRatio = (decimal) imageSizes[w][1] / imageSizes[w][0];
                }
                var h = (int)Math.Round(w * heightRatio);
                var cropString = urlHelper.GetCropUrl(publishedContent, w, h, "umbracoFile", quality: q, preferFocalPoint: true,
                    furtherOptions: Format(publishedContent, options), htmlEncode: false, imageCropMode: mode).ToString();

                outputStringBuilder.Append($"{cropString} {w}w,");
                w += WidthStep();
            }

            // remove the last comma
            var outputString = outputStringBuilder.ToString().Substring(0, outputStringBuilder.Length - 1);

            return new HtmlString(HttpUtility.HtmlEncode(outputString));
        }


        // fixed focalpoint picture, width, height ( fixed size image)
        public static IHtmlString GetFixedCropUrl(this UrlHelper urlHelper, IPublishedContent publishedContent, int width, int height, Dictionary<string, string> options = null, ImageCropMode cropmode = ImageCropMode.Crop)
        {
            var q = publishedContent.HasProperty("quality") &&
                    publishedContent.Value<int>("quality") > 0
                ? publishedContent.Value<int>("quality")
                : DefaultQuality();
            // niet croppen indien value aanstaat en crop gevraagd wordt
            var mode = publishedContent.HasProperty("doNotCrop") &&
                       publishedContent.Value<bool>("doNotCrop") && cropmode == ImageCropMode.Crop
                ? ImageCropMode.Max
                : cropmode;


            // remove the last comma
            var outputString = urlHelper.GetCropUrl(publishedContent, width, height, "umbracoFile", quality: q, preferFocalPoint: true,
                furtherOptions: Format(publishedContent, options), htmlEncode: false, imageCropMode: mode).ToString();

            return new HtmlString(HttpUtility.HtmlEncode(outputString));
        }

        // from url
        public static IHtmlString GetFixedCropUrlFromUrl(this UrlHelper urlHelper, string url, int width, int height, int? quality, Dictionary<string, string> options = null, ImageCropMode cropmode = ImageCropMode.Crop)
        {
            var q = quality.HasValue
                ? quality
                : DefaultQuality();
            Uri uri = new Uri(url);
            string cropurl = "/remote.axd/" + uri.Host + uri.AbsolutePath;
            // remove the last comma
            var outputString = urlHelper.GetCropUrl(cropurl, width, height, quality: q, preferFocalPoint: true,
                furtherOptions: Format(null, options), htmlEncode: false, imageCropMode: cropmode).ToString();

            return new HtmlString(HttpUtility.HtmlEncode(outputString));
        }

        public static IHtmlString GetSrcSetUrlsFromUrl(this UrlHelper urlHelper, string url, int width, int height, int? quality, Dictionary<string, string> options = null, ImageCropMode cropmode = ImageCropMode.Crop)
        {
            Uri uri = new Uri(url);
            string cropurl = "/remote.axd/" + uri.Host + uri.AbsolutePath;
            var w = WidthStep();
            var q = quality.HasValue
                ? quality
                : DefaultQuality();

            var outputStringBuilder = new StringBuilder();
            var heightRatio = (decimal)height / width;

            while (w <= MaxWidth(null))
            {
                var h = (int)Math.Round(w * heightRatio);
                var cropString = urlHelper.GetCropUrl(cropurl, w, h, quality: q, preferFocalPoint: true,
                    furtherOptions: Format(null, options), htmlEncode: false, imageCropMode: cropmode).ToString();

                outputStringBuilder.Append($"{cropString} {w}w,");
                w += WidthStep();
            }

            // remove the last comma
            var outputString = outputStringBuilder.ToString().Substring(0, outputStringBuilder.Length - 1);

            return new HtmlString(HttpUtility.HtmlEncode(outputString));
        }


        #endregion

        #region Html Helpers

        /// <summary>
        /// Convert img to img srcset, extracts width and height from querystrings
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="publishedContent"></param>
        /// <param name="propertyAlias">Alias of the TinyMce property</param>
        /// <param name="generateLqip">Set to true if you want LQIP markup to be generated</param>
        /// <param name="removeStyleAttribute">If you don't want the inline sytle attribute added by TinyMce to render</param>
        /// <param name="roundWidthHeight">Round width & height values as sometimes TinyMce adds decimal points</param>
        /// <returns>HTML Markup</returns>
        public static IHtmlString ConvertImgToSrcSet(this HtmlHelper htmlHelper, string sourceValueHtml, bool generateLqip = true)
        {
            var source = ConvertImgToSrcSetInternal(sourceValueHtml, generateLqip);

            // We have the raw value so we need to run it through the value converter to ensure that links and macros are rendered
            var rteConverter = new RteMacroRenderingValueConverter(Current.UmbracoContextAccessor, Current.Factory.GetAllInstances<IMacroRenderer>().FirstOrDefault());
            var intermediateValue = rteConverter.ConvertSourceToIntermediate(null, null, source, false);
            var objectValue = rteConverter.ConvertIntermediateToObject(null, null, 0, intermediateValue, false);

            return objectValue as IHtmlString;
        }
        /// <summary>
        /// Convert img to img srcset, extracts width and height from querystrings
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="html"></param>
        /// <param name="generateLqip"></param>
        /// <param name="removeStyleAttribute">If you don't want the inline sytle attribute added by TinyMce to render</param>
        /// <param name="removeUdiAttribute">If you don't want the inline data-udi attribute to render</param>
        /// <param name="roundWidthHeight">Round width & height values as sometimes TinyMce adds decimal points</param>
        /// <returns>HTML Markup</returns>
        private static IHtmlString ConvertImgToSrcSetInternal(string html, bool generateLqip = true, bool removeStyleAttribute = false, bool removeUdiAttribute = true, bool roundWidthHeight = true)
        {
            var urlHelper = new UrlHelper();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            if (!doc.ParseErrors.Any() && doc.DocumentNode != null)
            {
                // Find all images
                var imgNodes = doc.DocumentNode.SelectNodes("//img");

                if (imgNodes != null)
                {
                    var modified = false;

                    foreach (var img in imgNodes)
                    {
                        var srcAttr = img.Attributes.FirstOrDefault(x => x.Name == "src");
                        var udiAttr = img.Attributes.FirstOrDefault(x => x.Name == "data-udi");
                        var classAttr = img.Attributes.FirstOrDefault(x => x.Name == "class");

                        if (srcAttr != null)
                        {
                            // html decode the url as variables encoded in tinymce
                            var src = HttpUtility.HtmlDecode(srcAttr.Value);

                            var hasQueryString = src.InvariantContains("?");
                            NameValueCollection queryStringCollection;

                            if (hasQueryString)
                            {
                                queryStringCollection = HttpUtility.ParseQueryString(src.Substring(src.IndexOf('?')));

                                // ensure case of variables doesn't cause trouble
                                IDictionary<string, string> queryString = queryStringCollection.AllKeys.ToDictionary(k => k.ToLowerInvariant(), k => queryStringCollection[k]);

                                if (udiAttr != null)
                                {
                                    // Umbraco media
                                    GuidUdi guidUdi;
                                    if (GuidUdi.TryParse(udiAttr.Value, out guidUdi))
                                    {
                                        var node = GetAnyTypePublishedContent(guidUdi);

                                        var qsWidth = queryString["width"];
                                        var qsHeight = queryString["height"];

                                        // TinyMce sometimes adds decimals to image resize commands, we need to fix those
                                        if (decimal.TryParse(qsWidth, out decimal decWidth) && decimal.TryParse(qsHeight, out decimal decHeight))
                                        {
                                            var width = (int)Math.Round(decWidth);
                                            var height = (int)Math.Round(decHeight);

                                            // if width is 0 (I don't know why it would be but it has been seen) then we can't do anything
                                            if (width > 0)
                                            {
                                                // change the src attribute to data-src
                                                srcAttr.Name = "data-src";
                                                if (roundWidthHeight)
                                                {
                                                    var roundedUrl = urlHelper.GetCropUrl(node, width, height,
                                                        imageCropMode: ImageCropMode.Pad, preferFocalPoint: true);
                                                    srcAttr.Value = roundedUrl.ToString();
                                                }

                                                var srcSet = GetSrcSetUrls(urlHelper, node, width, height);

                                                img.Attributes.Add("data-srcset", srcSet.ToString());
                                                img.Attributes.Add("data-sizes", "auto");

                                                if (generateLqip)
                                                {
                                                    var imgLqip =
                                                        urlHelper.GetCropUrl(node, width, height, quality: 30,
                                                            furtherOptions: "&format=auto", preferFocalPoint: true);
                                                    img.Attributes.Add("src", imgLqip.ToString());
                                                }

                                                if (classAttr != null)
                                                {
                                                    classAttr.Value = $"{classAttr.Value} lazyload";
                                                }
                                                else
                                                {
                                                    img.Attributes.Add("class", "lazyload");
                                                }

                                                if (removeStyleAttribute)
                                                {
                                                    img.Attributes.Remove("style");
                                                }

                                                if (removeUdiAttribute)
                                                {
                                                    img.Attributes.Remove("data-udi");
                                                }

                                                modified = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Image in TinyMce doesn't have a data-udi attribute
                            }
                        }
                    }

                    if (modified)
                    {
                        return new HtmlString(doc.DocumentNode.OuterHtml);
                    }
                }
            }
            return new HtmlString(html);
        }

        #endregion

        #region Internal Functions

        private static int DefaultQuality()
        {
            var slimsyDefaultQuality = ConfigurationManager.AppSettings["Slimsy:DefaultQuality"];
            if (!int.TryParse(slimsyDefaultQuality, out int defaultQuality))
            {
                defaultQuality = 90;
            }

            return defaultQuality;
        }

        private static int WidthStep()
        {
            var slimsyWidthStep = ConfigurationManager.AppSettings["Slimsy:WidthStep"];
            if (!int.TryParse(slimsyWidthStep, out int widthStep))
            {
                widthStep = 160;
            }

            return widthStep;
        }

        private static int MaxWidth(IPublishedContent publishedContent)
        {
            var slimsyMaxWidth = ConfigurationManager.AppSettings["Slimsy:MaxWidth"];
            if (!int.TryParse(slimsyMaxWidth, out int maxWidth))
            {
                maxWidth = 2048;
            }

            // if publishedContent is a media item we can see if we can get the source image width & height
            if (publishedContent != null && publishedContent.ItemType == PublishedItemType.Media)
            {
                var sourceWidth = publishedContent.Value<int>(Constants.Conventions.Media.Width);

                // if source width is less than max width then we should stop at source width
                if (sourceWidth < maxWidth)
                {
                    maxWidth = sourceWidth;
                }

                // if the source image is less than the step then max width should be the first step
                if (maxWidth < WidthStep())
                {
                    maxWidth = WidthStep();
                }
            }

            return maxWidth;
        }

        private static string Format(IPublishedContent publishedContent, Dictionary<string, string> options = null)
        {
            string returnstring = "";
            if (publishedContent != null && publishedContent.HasProperty("type") &&
                !string.IsNullOrEmpty(publishedContent.Value<string>("type")))
            {

                returnstring += "&format=" + publishedContent.Value<string>("type");
            }
            if (!string.IsNullOrEmpty(returnstring) || options != null)
            {
                if (options != null)
                {
                    foreach (var option in options)
                    {
                        returnstring += "&" + option.Key + "=" + option.Value;
                    }
                }

                return returnstring;
            }
            return null;
        }

        private static IPublishedContent GetAnyTypePublishedContent(GuidUdi guidUdi)
        {
            switch (guidUdi.EntityType)
            {
                case Constants.UdiEntityType.Media:
                    return Current.UmbracoContext.MediaCache.GetById(guidUdi.Guid);
                    break;
                case Constants.UdiEntityType.Document:
                    return Current.UmbracoContext.ContentCache.GetById(guidUdi.Guid);
                    break;
                default:
                    return null;
            }
        }
        private static T GetLocalCacheItem<T>(string cacheKey)
        {
            var runtimeCache = Current.AppCaches.RuntimeCache;
            var cachedItem = runtimeCache.GetCacheItem<T>(cacheKey);
            return cachedItem;
        }

        private static void InsertLocalCacheItem<T>(string cacheKey, Func<T> getCacheItem)
        {
            var runtimeCache = Current.AppCaches.RuntimeCache;
            runtimeCache.InsertCacheItem<T>(cacheKey, getCacheItem);
        }

        #endregion
    }
}