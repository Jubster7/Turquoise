global _main
_main:
	push 255
	push 99
	push QWORD [rsp + 8]
	mov rax, 33554433
	pop rdi
	syscall
	mov rax, 33554433
	mov rdi, 0
	syscall