/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironruby@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

#if !CLR2
using MSA = System.Linq.Expressions;
#else
using MSA = Microsoft.Scripting.Ast;
#endif

using Microsoft.Scripting;

namespace IronRuby.Compiler.Ast {
    using Ast = MSA.Expression;
    using AstUtils = Microsoft.Scripting.Ast.Utils;

    public partial class ReturnStatement : JumpStatement {
        public ReturnStatement(Arguments arguments, SourceSpan location)
            : base(arguments, location) {
        }

        // see Ruby Language.doc/Runtime/Control Flow Implementation/Return
        internal override MSA.Expression/*!*/ Transform(AstGenerator/*!*/ gen) {

            MSA.Expression transformedReturnValue = TransformReturnValue(gen);

            // eval:
            if (gen.CompilerOptions.IsEval) {
                return gen.Return(Methods.EvalReturn.OpCall(gen.CurrentScopeVariable, AstUtils.Box(transformedReturnValue)));
            }

            // block:
            if (gen.CurrentBlock != null) {
                return gen.Return(Methods.BlockReturn.OpCall(gen.CurrentBlock.BfcVariable, AstUtils.Box(transformedReturnValue)));
            }

            // method:
            return gen.Return(transformedReturnValue);
        }
    }
}
