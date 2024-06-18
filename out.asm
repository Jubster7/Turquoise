global _main
_main:
	push 99
	push 1
	pop rax
	test rax, rax
	jz label1
	push 1
	pop rax
	test rax, rax
	jz label3
	jmp label2
label3:
	push 3
	mov rax, 33554433
	pop rdi
	syscall
label2:
	jmp label0
label1:
label0:
	push QWORD [rsp + 0]
	mov rax, 33554433
	pop rdi
	syscall
	mov rax, 33554433
	mov rdi, 0
	syscall