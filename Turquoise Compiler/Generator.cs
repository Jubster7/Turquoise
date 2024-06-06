namespace Compiler;

static class Generator {
	public static string Generate(NodeExit root_node) {
		string output = "global _start\n_start:\n";
		output += "\tmov rax, 60\n";
		output += "\tmov rdi, " + root_node.expression.int_literal.value + "\n";
		output += "\tsyscall";
		return output;
	}
}