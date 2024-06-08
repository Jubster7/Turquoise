using System.Diagnostics.Contracts;

namespace Compiler;
using OneOf;

static class Generator {
	[Pure] public static string Generate(NodeProgram root) {
		string entry_point_name = Program.assembly_entry_point_label;
		string output = $"global {entry_point_name}\n{entry_point_name}:\n";

		void GenerateExpression(NodeExpression nodeExpression) {
			nodeExpression.expression.Switch(
				NodeExpressionIntLiteral => {
					output += "\tmov rax, " + NodeExpressionIntLiteral.int_literal.value + "\n";
					output += "\tpush rax\n";
				},
				NodeExpressionIdentifier => {
					//TODO
				}
			);
		}

		void GenerateStatement(in NodesStatement nodesStatement) {
			nodesStatement.statement.Switch(
				NodeStatementExit => {
					GenerateExpression(NodeStatementExit.expression);
					output += "\tmov rax, 0x2000001\n";
					output += "\tpop rdi\n";
					output += "\tsyscall\n";
				},
				NodeStatementVar => {
					output += "";
				}
			);
		}

		foreach (NodesStatement nodesStatement in root.statements) {
			GenerateStatement(nodesStatement);
		}


		output += "\tmov rax, 0x2000001\n";
		output += "\tmov rdi, 0\n";
		output += "\tsyscall";
		return output;
	}
}