$$
\begin{align*}

\text{Program} &\rightarrow \text{[statement]}^* \\

[\text{statement}] &\rightarrow
\begin{cases}
	\text{[exit]} \rightarrow exit(\text{[expression]}); \\
\end{cases}\\

[\text{expression}] &\rightarrow
\begin{cases}
	\text{[int\_literal]}\\
	\text{[identifier]} \\
	\text{[binary\_expression]} \\
\end{cases}\\

[\text{binary\_expression}] &\rightarrow
\begin{cases}
\begin{align*}
	\text{[Multiply]} &\rightarrow \text{[expression]} \times \text{[expression]}\  \text{precedence} = 0\\
	\text{[Division]} &\rightarrow \text{[expression]} \div \text{[expression]}\\
	\text{[Add]} &\rightarrow \text{[expression]} + \text{[expression]}\
	\text{precedence} = 1\\
	\text{[Subtraction]} &\rightarrow \text{[expression]} - \text{[expression]}\\
\end{align*}
\end{cases}


\end{align*}
$$