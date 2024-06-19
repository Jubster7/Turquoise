global _main
_main:
	push 99
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
	push 2
	mov rax, 33554433
	pop rdi
	syscall
label0:
	push 0
	pop rax
	test rax, rax
	jz label3
	push 3
	mov rax, 33554433
	pop rdi
	syscall
	jmp label2
label3:
label2:
	push QWORD [rsp + 0]
	mov rax, 33554433
	pop rdi
	syscall
	mov rax, 33554433
	mov rdi, 0
	syscall