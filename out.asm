global _main
_main:
	push 1
	push 2
	push 3
	pop rax
	pop rbx
	mul rbx
	push rax
	pop rax
	pop rbx
	add rax, rbx
	push rax
	push 1
	push QWORD [rsp + 8]
	mov rax, 33554433
	pop rdi
	syscall
	mov rax, 33554433
	mov rdi, 0
	syscall