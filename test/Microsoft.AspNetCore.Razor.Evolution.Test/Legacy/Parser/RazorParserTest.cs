// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    public class RazorParserTest
    {
        [Fact]
        public void CanParseStuff()
        {
            var parser = new RazorParser();
            var filePath = "TestFiles/Source/BasicMarkup.cshtml";
            var fileStream = File.OpenRead(filePath);
            var sourceDocument = RazorSourceDocument.ReadFrom(fileStream, filePath);
            var documentReader = sourceDocument.CreateReader();
            var output = parser.Parse(documentReader);

            Assert.NotNull(output);
        }

        [Fact]
        public void ParseMethodCallsParseDocumentOnMarkupParserAndReturnsResults()
        {
            var factory = new SpanFactory();

            // Arrange
            var parser = new RazorParser();

            // Act/Assert
            ParserTestBase.EvaluateResults(parser.Parse(new StringReader("foo @bar baz")),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ParseMethodUsesProvidedParserListenerIfSpecified()
        {
            var factory = new SpanFactory();

            // Arrange
            var parser = new RazorParser();

            // Act
            var results = parser.Parse(new StringReader("foo @bar baz"));

            // Assert
            ParserTestBase.EvaluateResults(results,
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }
    }
}
