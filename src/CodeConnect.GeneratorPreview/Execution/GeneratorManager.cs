﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnect.GeneratorPreview.View;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Collections.Immutable;
using System.Threading;

namespace CodeConnect.GeneratorPreview.Execution
{
    public class GeneratorManager
    {
        private BaseMethodDeclarationSyntax _generator;
        private BaseMethodDeclarationSyntax _target;
        private Document _targetDocument;
        private PreviewWindowPackage _previewWindowPackage;
        private IViewModel _viewModel;

        public GeneratorManager(PreviewWindowPackage previewWindowPackage, IViewModel viewModel)
        {
            _previewWindowPackage = previewWindowPackage;
            _viewModel = viewModel;
        }

        public void SetGenerator(BaseMethodDeclarationSyntax method)
        {
            _generator = method;
            _viewModel.GeneratorName = getUIName(method);
        }

        public void SetTarget(BaseMethodDeclarationSyntax method, Document document)
        {
            _target = method;
            _targetDocument = document;
            _viewModel.TargetName = $"{getUIName(method)} at {document.Name}";
        }

        public async Task Generate(CancellationToken token = default(CancellationToken))
        {
            if (_target == null)
                throw new InvalidOperationException("Pick the target method.");
            if (_targetDocument == null)
                throw new InvalidOperationException("Target document is not set.");
            if (_generator == null)
                throw new InvalidOperationException("Pick the generator.");

            var compilation = await _targetDocument.Project.GetCompilationAsync(token);

            var generator = new MyGenerator(context => context.AddCompilationUnit("__c", CSharpSyntaxTree.ParseText(@"class __C { }")));

            var path = Path.GetDirectoryName(_targetDocument.FilePath);

            var trees = compilation.GenerateSource(
                ImmutableArray.Create<SourceGenerator>(generator),
                path,
                writeToDisk: false,
                cancellationToken: token);
            var filePath = Path.Combine(path, "__c.cs");

            Microsoft.CodeAnalysis.Text.SourceText sourceText;
            if (trees[0].TryGetText(out sourceText))
            {
                // Update the solution
                var newSolution = _targetDocument.Project.Solution.AddDocument(DocumentId.CreateNewId(_targetDocument.Project.Id, "__c"), "__c", sourceText, null, filePath, true);
                var newProject = _targetDocument.Project.AddDocument("__c", sourceText, null, filePath);
            }

            // 2. figure out how to create an instance of generator
            // 3. get compiled trees and return the bit with the target method

            _viewModel.GeneratedCode = _target.ToFullString();
            _viewModel.Errors = String.Join(Environment.NewLine, trees[0].GetDiagnostics().Select(n => n.ToString()));
        }

        private string getUIName(BaseMethodDeclarationSyntax baseMethod)
        {
            var method = baseMethod as MethodDeclarationSyntax;
            if (method != null)
            {
                return method.Identifier.ToString();
            }
            else
            {
                return baseMethod.GetType().ToString();
            }
        }

        private sealed class MyGenerator : SourceGenerator
        {
            private readonly Action<SourceGeneratorContext> _execute;

            internal MyGenerator(Action<SourceGeneratorContext> execute)
            {
                _execute = execute;
            }

            public override void Execute(SourceGeneratorContext context)
            {
                _execute(context);
            }
        }
    }
}
