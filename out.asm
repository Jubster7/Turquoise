global _main
_main:
	push 0
	pop rax
	test rax, rax
	jz label1
	push 1
	mov rax, 33554433
	pop rdi
	syscall
	jmp label0
label1:
label0:
	push 0
	pop rax
	test rax, rax
	jz label3
	jmp label2
label3:
label2:
	mov rax, 33554433
	mov rdi, 0
	syscall