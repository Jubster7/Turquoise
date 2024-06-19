global _main
_main:
	push 99
	push 0
	pop rax
	test rax, rax
	jz label1
	jmp label0
label1:
	push 0
	pop rax
	test rax, rax
	jz label2
	jmp label0
label2:
label0:
	push QWORD [rsp + 0]
	mov rax, 33554433
	pop rdi
	syscall
	mov rax, 33554433
	mov rdi, 0
	syscall