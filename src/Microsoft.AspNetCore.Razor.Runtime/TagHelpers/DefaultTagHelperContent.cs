// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    /// <summary>
    /// Default concrete <see cref="TagHelperContent"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public class DefaultTagHelperContent : TagHelperContent
    {
        private object _singleContent;
        private bool _singleContentSet;
        private List<object> _buffer;
        private bool _isModified;

        private List<object> Buffer
        {
            get
            {
                _isModified = true;

                if (_buffer == null)
                {
                    _buffer = new List<object>();
                }

                return _buffer;
            }
        }

        /// <inheritdoc />
        public override bool IsModified => _isModified;

        /// <inheritdoc />
        /// <remarks>Returns <c>true</c> for a cleared <see cref="TagHelperContent"/>.</remarks>
        public override bool IsWhiteSpace
        {
            get
            {
                if (!IsModified)
                {
                    return true;
                }

                using (var writer = new EmptyOrWhiteSpaceWriter())
                {
                    if (_singleContentSet)
                    {
                        var entry = _singleContent;
                        if (entry == null)
                        {
                            return true;
                        }

                        var stringValue = entry as string;
                        if (stringValue != null)
                        {
                            if (!string.IsNullOrWhiteSpace(stringValue))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            ((IHtmlContent)entry).WriteTo(writer, HtmlEncoder.Default);
                            if (!writer.IsWhiteSpace)
                            {
                                return false;
                            }
                        }
                    }

                    foreach (var entry in _buffer)
                    {
                        if (entry == null)
                        {
                            continue;
                        }

                        var stringValue = entry as string;
                        if (stringValue != null)
                        {
                            if (!string.IsNullOrWhiteSpace(stringValue))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            ((IHtmlContent)entry).WriteTo(writer, HtmlEncoder.Default);
                            if (!writer.IsWhiteSpace)
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
        }

        /// <inheritdoc />
        public override bool IsEmpty
        {
            get
            {
                if (!IsModified)
                {
                    return true;
                }

                using (var writer = new EmptyOrWhiteSpaceWriter())
                {
                    if (_singleContentSet)
                    {
                        var entry = _singleContent;
                        if (entry == null)
                        {
                            return true;
                        }

                        var stringValue = entry as string;
                        if (stringValue != null)
                        {
                            if (!string.IsNullOrEmpty(stringValue))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            ((IHtmlContent)entry).WriteTo(writer, HtmlEncoder.Default);
                            if (!writer.IsEmpty)
                            {
                                return false;
                            }
                        }
                    }

                    foreach (var entry in _buffer)
                    {
                        if (entry == null)
                        {
                            continue;
                        }

                        var stringValue = entry as string;
                        if (stringValue != null)
                        {
                            if (!string.IsNullOrEmpty(stringValue))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            ((IHtmlContent)entry).WriteTo(writer, HtmlEncoder.Default);
                            if (!writer.IsEmpty)
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
        }

        public override TagHelperContent SetContent(IHtmlContent htmlContent) => SetContentItem(htmlContent);

        public override TagHelperContent SetContent(string unencoded) => SetContentItem(unencoded);

        private TagHelperContent SetContentItem(object item)
        {
            _isModified = true;
            _buffer?.Clear();
            _singleContent = item;
            _singleContentSet = true;

            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent Append(string unencoded)
        {
            return AppendItem(unencoded);
        }

        /// <inheritdoc />
        public override TagHelperContent AppendHtml(string encoded)
        {
            if (encoded == null)
            {
                AppendItem(null);
            }
            else
            {
                AppendHtml(new HtmlEncodedString(encoded));
            }
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendHtml(IHtmlContent htmlContent)
        {
            return AppendItem(htmlContent);
        }

        private TagHelperContent AppendItem(object item)
        {
            if (_singleContentSet)
            {
                Buffer.Add(_singleContent);
                _singleContentSet = false;
            }

            Buffer.Add(item);

            return this;
        }

        /// <inheritdoc />
        public override void CopyTo(IHtmlContentBuilder destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (!IsModified)
            {
                return;
            }

            if (_singleContentSet)
            {
                var entry = _singleContent;
                if (entry == null)
                {
                    return;
                }

                string entryAsString;
                IHtmlContentContainer entryAsContainer;
                if ((entryAsString = entry as string) != null)
                {
                    destination.Append(entryAsString);
                }
                else if ((entryAsContainer = entry as IHtmlContentContainer) != null)
                {
                    entryAsContainer.CopyTo(destination);
                }
                else
                {
                    destination.AppendHtml((IHtmlContent)entry);
                }

                return;
            }

            for (var i = 0; i < Buffer.Count; i++)
            {
                var entry = Buffer[i];
                if (entry == null)
                {
                    continue;
                }

                string entryAsString;
                IHtmlContentContainer entryAsContainer;
                if ((entryAsString = entry as string) != null)
                {
                    destination.Append(entryAsString);
                }
                else if ((entryAsContainer = entry as IHtmlContentContainer) != null)
                {
                    entryAsContainer.CopyTo(destination);
                }
                else
                {
                    destination.AppendHtml((IHtmlContent)entry);
                }
            }
        }

        /// <inheritdoc />
        public override void MoveTo(IHtmlContentBuilder destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (!IsModified)
            {
                return;
            }

            if (_singleContentSet)
            {
                var entry = _singleContent;
                if (entry == null)
                {
                    return;
                }

                string entryAsString;
                IHtmlContentContainer entryAsContainer;
                if ((entryAsString = entry as string) != null)
                {
                    destination.Append(entryAsString);
                }
                else if ((entryAsContainer = entry as IHtmlContentContainer) != null)
                {
                    entryAsContainer.MoveTo(destination);
                }
                else
                {
                    destination.AppendHtml((IHtmlContent)entry);
                }

                return;
            }

            for (var i = 0; i < Buffer.Count; i++)
            {
                var entry = Buffer[i];
                if (entry == null)
                {
                    continue;
                }

                string entryAsString;
                IHtmlContentContainer entryAsContainer;
                if ((entryAsString = entry as string) != null)
                {
                    destination.Append(entryAsString);
                }
                else if ((entryAsContainer = entry as IHtmlContentContainer) != null)
                {
                    entryAsContainer.MoveTo(destination);
                }
                else
                {
                    destination.AppendHtml((IHtmlContent)entry);
                }
            }

            Clear();
        }

        /// <inheritdoc />
        public override TagHelperContent Clear()
        {
            _singleContentSet = false;
            _isModified = true;
            _buffer?.Clear();
            return this;
        }

        /// <inheritdoc />
        public override string GetContent()
        {
            return GetContent(HtmlEncoder.Default);
        }

        /// <inheritdoc />
        public override string GetContent(HtmlEncoder encoder)
        {
            if (!IsModified)
            {
                return string.Empty;
            }

            using (var writer = new StringWriter())
            {
                WriteTo(writer, encoder);
                return writer.ToString();
            }
        }

        /// <inheritdoc />
        public override void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            if (!IsModified)
            {
                return;
            }

            if (_singleContentSet)
            {
                var entry = _singleContent;
                if (entry == null)
                {
                    return;
                }

                var stringValue = entry as string;
                if (stringValue != null)
                {
                    encoder.Encode(writer, stringValue);
                }
                else
                {
                    ((IHtmlContent)entry).WriteTo(writer, encoder);
                }

                return;
            }

            foreach (var entry in _buffer)
            {
                if (entry == null)
                {
                    continue;
                }

                var stringValue = entry as string;
                if (stringValue != null)
                {
                    encoder.Encode(writer, stringValue);
                }
                else
                {
                    ((IHtmlContent)entry).WriteTo(writer, encoder);
                }
            }
        }

        private string DebuggerToString()
        {
            return GetContent();
        }

        public override void Reset()
        {
            _isModified = false;
            _singleContentSet = false;
            _buffer?.Clear();
        }

        // Overrides Write(string) to find if the content written is empty/whitespace.
        private class EmptyOrWhiteSpaceWriter : TextWriter
        {
            public override Encoding Encoding
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsEmpty { get; private set; } = true;

            public bool IsWhiteSpace { get; private set; } = true;

#if NETSTANDARD1_3
            // This is an abstract method in DNXCore
            public override void Write(char value)
            {
                throw new NotImplementedException();
            }
#endif

            public override void Write(string value)
            {
                if (IsEmpty && !string.IsNullOrEmpty(value))
                {
                    IsEmpty = false;
                }

                if (IsWhiteSpace && !string.IsNullOrWhiteSpace(value))
                {
                    IsWhiteSpace = false;
                }
            }
        }
    }
}