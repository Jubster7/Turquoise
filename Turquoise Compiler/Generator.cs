using System.Diagnostics.Contracts;

namespace Compiler;
using OneOf;

static class Generator {
	public static string Generate(NodeProgram root) {
		string entry_point_name = Program.assembly_entry_point_label;
		string output = $"global {entry_point_name}\n{entry_point_name}:\n";

		string GenerateExpression(NodeExpression nodeExpression) {
			string output = "";
			nodeExpression.expression.Switch(
				NodeExpressionIntLiteral => {
				},
				NodeExpressionIdentifier => {
				}
			);
			return output;
		}

		[Pure] static string GenerateStatement(in NodesStatement nodesStatement) {
			string output = "";
			nodesStatement.statement.Switch(
				NodeStatementExit => {
					Console.WriteLine("NodeStatementExit");
					output += "";
				},
				NodeStatementVar => {
					Console.WriteLine("NodeStatementVar");
					output += "";
				}
			);
			return output;
		}

		foreach (NodesStatement nodesStatement in root.statements) {
			output += GenerateStatement(nodesStatement);
		}


		output += "\tmov rax, 60\n";
		output += "\tmov rdi, 0\n";
		output += "\tsyscall";
		return output;
	}
}