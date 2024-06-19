global _main
_main:
	push 2
	push 3
	push 2
	pop rax
	pop rbx
	mul rbx
	push rax
	push 10
	pop rax
	pop rbx
	sub rax, rbx
	push rax
	pop rax
	pop rbx
	mov rdx, 0
	div rbx
	push rax
	push 99
	push 0
	pop rax
	test rax, rax
	jz label0
label0:
	push QWORD [rsp + 0]
	mov rax, 33554433
	pop rdi
	syscall
	mov rax, 33554433
	mov rdi, 0
	syscall