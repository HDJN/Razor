﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorIRLoweringPhase : RazorEnginePhaseBase, IRazorIRLoweringPhase
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDependency(syntaxTree);

            var visitor = new Visitor();

            visitor.VisitBlock(syntaxTree.Root);

            var irDocument = (DocumentIRNode)visitor.Builder.Build();
            codeDocument.SetIRDocument(irDocument);
        }

        private class Visitor : ParserVisitor
        {
            private readonly Stack<RazorIRBuilder> _builders;

            public Visitor()
            {
                _builders = new Stack<RazorIRBuilder>();
                var document = RazorIRBuilder.Document();
                _builders.Push(document);

                Namespace = new NamespaceDeclarationIRNode();
                Builder.Push(Namespace);

                Class = new ClassDeclarationIRNode();
                Builder.Push(Class);

                Method = new RazorMethodDeclarationIRNode();
                Builder.Push(Method);
            }

            public RazorIRBuilder Builder => _builders.Peek();

            public NamespaceDeclarationIRNode Namespace { get; }

            public ClassDeclarationIRNode Class { get; }

            public RazorMethodDeclarationIRNode Method { get; }

            public override void VisitStartAttributeBlock(AttributeBlockChunkGenerator chunk, Block block)
            {
                var value = new ContainerRazorIRNode();
                Builder.Add(new HtmlAttributeIRNode()
                {
                    Name = chunk.Name,
                    Prefix = chunk.Prefix,
                    Value = value,
                    Suffix = chunk.Suffix,

                    SourceLocation = block.Start,
                });

                var valueBuilder = RazorIRBuilder.Create(value);
                _builders.Push(valueBuilder);
            }

            public override void VisitEndAttributeBlock(AttributeBlockChunkGenerator chunk, Block block)
            {
                _builders.Pop();
            }

            public override void VisitStartDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunk, Block block)
            {
                var value = new ContainerRazorIRNode();
                Builder.Add(new CSharpAttributeValueIRNode()
                {
                    Prefix = chunk.Prefix,
                    Value = value,
                    SourceLocation = block.Start,
                });

                var valueBuilder = RazorIRBuilder.Create(value);
                _builders.Push(valueBuilder);
            }

            public override void VisitEndDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunk, Block block)
            {
                _builders.Pop();
            }

            public override void VisitLiteralAttributeSpan(LiteralAttributeChunkGenerator chunk, Span span)
            {
                Builder.Add(new HtmlAttributeValueIRNode()
                {
                    Prefix = chunk.Prefix,
                    Value = span.Content,
                    SourceLocation = span.Start,
                });
            }

            public override void VisitStartTemplateBlock(TemplateBlockChunkGenerator chunk, Block block)
            {
                Builder.Push(new TemplateIRNode());
            }

            public override void VisitEndTemplateBlock(TemplateBlockChunkGenerator chunk, Block block)
            {
                Builder.Pop();
            }

            // CSharp expressions are broken up into blocks and spans because Razor allows Razor comments
            // inside an expression.
            // Ex:
            //      @DateTime.@*This is a comment*@Now
            //
            // We need to capture this in the IR so that we can give each piece the correct source mappings
            public override void VisitStartExpressionBlock(ExpressionChunkGenerator chunk, Block block)
            {
                var value = new ContainerRazorIRNode();
                Builder.Add(new CSharpExpressionIRNode()
                {
                    Expression = value,
                    SourceLocation = block.Start,
                });

                var valueBuilder = RazorIRBuilder.Create(value);
                _builders.Push(valueBuilder);
            }

            public override void VisitEndExpressionBlock(ExpressionChunkGenerator chunk, Block block)
            {
                _builders.Pop();
            }

            public override void VisitExpressionSpan(ExpressionChunkGenerator chunk, Span span)
            {
                Builder.Add(new CSharpTokenIRNode()
                {
                    Content = span.Content,
                    SourceLocation = span.Start,
                });
            }

            public override void VisitTypeMemberSpan(TypeMemberChunkGenerator chunk, Span span)
            {
                var functionsNode = new CSharpStatementIRNode()
                {
                    Content = span.Content,
                    SourceLocation = span.Start,
                    Parent = Class,
                };

                Class.Children.Add(functionsNode);
            }

            public override void VisitStatementSpan(StatementChunkGenerator chunk, Span span)
            {
                Builder.Add(new CSharpStatementIRNode()
                {
                    Content = span.Content,
                    SourceLocation = span.Start,
                });
            }

            public override void VisitMarkupSpan(MarkupChunkGenerator chunk, Span span)
            {
                var currentChildren = Builder.Current.Children;
                if (currentChildren.Count > 0 && currentChildren[currentChildren.Count - 1] is HtmlContentIRNode)
                {
                    var existingHtmlContent = (HtmlContentIRNode)currentChildren[currentChildren.Count - 1];
                    existingHtmlContent.Content = string.Concat(existingHtmlContent.Content, span.Content);
                }
                else
                {
                    Builder.Add(new HtmlContentIRNode()
                    {
                        Content = span.Content,
                        SourceLocation = span.Start,
                    });
                }
            }

            public override void VisitImportSpan(AddImportChunkGenerator chunk, Span span)
            {
                // For prettiness, let's insert the usings before the class declaration.
                var i = 0;
                for (; i < Namespace.Children.Count; i++)
                {
                    if (Namespace.Children[i] is ClassDeclarationIRNode)
                    {
                        break;
                    }
                }

                var @using = new UsingStatementIRNode()
                {
                    Content = span.Content,
                    Parent = Namespace,
                    SourceLocation = span.Start,
                };

                Namespace.Children.Insert(i, @using);
            }

            private class ContainerRazorIRNode : RazorIRNode
            {
                private SourceLocation? _location;

                public override IList<RazorIRNode> Children { get; } = new List<RazorIRNode>();

                public override RazorIRNode Parent { get; set; }

                internal override SourceLocation SourceLocation
                {
                    get
                    {
                        if (_location == null)
                        {
                            if (Children.Count > 0)
                            {
                                return Children[0].SourceLocation;
                            }

                            return SourceLocation.Undefined;
                        }

                        return _location.Value;
                    }
                    set
                    {
                        _location = value;
                    }
                }

                public override void Accept(RazorIRNodeVisitor visitor)
                {
                    visitor.VisitDefault(this);
                }

                public override TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor)
                {
                    return visitor.VisitDefault(this);
                }
            }
        }
    }
}
