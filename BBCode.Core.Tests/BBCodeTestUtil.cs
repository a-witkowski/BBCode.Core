﻿using CodeKicker.BBCode.Core.SyntaxTree;
using RandomTestValues;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeKicker.BBCode.Core.Tests
{
    public static class BBCodeTestUtil
    {
        public static SequenceNode CreateRootNode(BBTag[] allowedTags)
        {
            var node = new SequenceNode();
            AddSubnodes(allowedTags, node);
            return node;
        }
        static SyntaxTreeNode CreateNode(BBTag[] allowedTags, bool allowText)
        {
            switch (new[] { allowText ? 0 : 1, 2 }[RandomValue.Int(1, 0)])
            {
                case 0:
                    var text = RandomValue.String();
                    return new TextNode(text);
                case 1:
                    var tag = allowedTags[RandomValue.Int(allowedTags.Length, 0)];
                    var node = new TagNode(tag);

                    AddSubnodes(allowedTags, node);

                    if (tag.Attributes != null)
                    {
                        var selectedIds = new List<string>();
                        foreach (var attr in tag.Attributes)
                        {
                            if (!selectedIds.Contains(attr.ID) && RandomValue.Bool())
                            {
                                string val;
                                do
                                {
                                    val = RandomValue.String();
                                } while (val.IndexOfAny("[] ".ToCharArray()) != -1);

                                node.AttributeValues[attr] = val;
                                selectedIds.Add(attr.ID);
                            }
                        }
                    }
                    return node;
                default:
                    return null;
            }
        }
        static void AddSubnodes(BBTag[] allowedTags, SyntaxTreeNode node)
        {
            int count = RandomValue.Int(3, 0);
            bool lastWasText = false;
            for (int i = 0; i < count; i++)
            {
                var subNode = CreateNode(allowedTags, !lastWasText);
                lastWasText = subNode is TextNode;
                node.SubNodes.Add(subNode);
            }
        }

        public static BBCodeParser GetParserForTest(ErrorMode errorMode, bool includePlaceholder, BBTagClosingStyle listItemBBTagClosingStyle, bool enableIterationElementBehavior)
        {
            return new BBCodeParser(errorMode, null, new[]
                {
                    new BBTag("b", "<b>", "</b>"),
                    new BBTag("i", "<span style=\"font-style:italic;\">", "</span>"),
                    new BBTag("u", "<span style=\"text-decoration:underline;\">", "</span>"),
                    new BBTag("code", "<pre class=\"prettyprint\">", "</pre>"),
                    new BBTag("img", "<img src=\"${content}\" />", "", false, true),
                    new BBTag("quote", "<blockquote>", "</blockquote>"),
                    new BBTag("list", "<ul>", "</ul>"),
                    new BBTag("*", "<li>", "</li>", true, listItemBBTagClosingStyle, null, enableIterationElementBehavior),
                    new BBTag("url", "<a href=\"${href}\">", "</a>", new BBAttribute("href", ""), new BBAttribute("href", "href")),
                    new BBTag("url2", "<a href=\"${href}\">", "</a>", new BBAttribute("href", "", GetUrl2Href), new BBAttribute("href", "href", GetUrl2Href)),
                    !includePlaceholder ? null : new BBTag("placeholder", "${name}", "", false, BBTagClosingStyle.LeafElementWithoutContent, null, true, new BBAttribute("name", "", name => "xxx" + name.AttributeValue + "yyy")),
                }.Where(x => x != null).ToArray());
        }

        public static BBCodeParser GetCustomParser()
        {
            string urlTransformer(string url)
            {
                if (!url.StartsWith("www", StringComparison.InvariantCultureIgnoreCase) && !url.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                {
                    //return "";
                    throw new ArgumentException("Bad URL formatting");
                }
                else if (url.StartsWith("www", StringComparison.InvariantCultureIgnoreCase))
                {
                    url = $"//{url}";
                }
                return url;
            }

            return new BBCodeParser(new[]
                {
                    new BBTag("b", "<b>", "</b>"),
                    new BBTag("i", "<span style=\"font-style:italic;\">", "</span>"),
                    new BBTag("u", "<span style=\"text-decoration:underline;\">", "</span>"),
                    new BBTag("code", "<pre class=\"prettyprint\">", "</pre>"),
                    new BBTag("img", "<img src=\"${content}\" />", "", false, true),
                    new BBTag("quote", "<blockquote>", "</blockquote>"),
                    new BBTag("list", "<ul>", "</ul>"),
                    new BBTag("*", "<li>", "</li>", true, BBTagClosingStyle.AutoCloseElement, null, true),
                    new BBTag("url", "<a href=\"${href}\">", "</a>", new BBAttribute("href", ""), new BBAttribute("href", "href")),
                    new BBTag("url2", "<a href=\"${href}\">", "</a>", new BBAttribute("href", "", GetUrl2Href), new BBAttribute("href", "href", GetUrl2Href)),
                    new BBTag("color", "<span style=\"color:${code}\">", "</span>", new BBAttribute("code", "")),
                    new BBTag("attachment", "#{AttachmentFileName=${content}/AttachmentIndex=${num}}#", "", false, true, new BBAttribute("num", ""))
                }.Where(x => x != null).ToArray());
        }

        static string GetUrl2Href(IAttributeRenderingContext attributeRenderingContext)
        {
            if (!string.IsNullOrEmpty(attributeRenderingContext.AttributeValue)) return attributeRenderingContext.AttributeValue;

            var content = attributeRenderingContext.GetAttributeValueByID(BBTag.ContentPlaceholderName);
            if (!string.IsNullOrEmpty(content) && content.StartsWith("http:")) return content;

            return null;
        }

        public static BBCodeParser GetSimpleParserForTest(ErrorMode errorMode)
        {
            return new BBCodeParser(errorMode, null, new[]
                {
                    new BBTag("x", "${content}${x}", "${y}", true, true, new BBAttribute("x", "x"), new BBAttribute("y", "y", x => x.AttributeValue)),
                });
        }

        public static string SimpleBBEncodeForTest(string bbCode, ErrorMode errorMode)
        {
            return GetSimpleParserForTest(errorMode).ToHtml(bbCode);
        }

        public static bool IsValid(string bbCode, ErrorMode errorMode)
        {
            try
            {
                BBCodeParserTest.BBEncodeForTest(bbCode, errorMode);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static SequenceNode GetAnyTree()
        {
            var parser = GetParserForTest(RandomValue.Object<ErrorMode>(), true, RandomValue.Object<BBTagClosingStyle>(), false);
            return CreateRootNode(parser.Tags.ToArray());
        }
    }
}