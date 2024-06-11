$$
\begin{align*}

\text{Program} &\rightarrow [\text{statement}]^* \\

[\text{Statement}] &\rightarrow
\begin{cases}
	exit([\text{Expression}]); \\
	var\space [\text{identifier}] = [\text{Expression}];
\end{cases}\\

[\text{Expression}] &\rightarrow
\begin{cases}
	[\text{Term}]\\
	[\text{Binary\_expression}] \\
\end{cases}\\

[\text{Binary\_expression}] &\rightarrow
\begin{cases}
	[\text{Expression}] * [\text{Expression}] &\text{precedence} = 1\\
	[\text{Expression}]\space /\space  [\text{Expression}]\\
	[\text{Expression}] + [\text{Expression}] &\text{precedence} = 0\\
	[\text{Expression}] - \text{[Expression]}\\
\end{cases}\\

[\text{Term}] &\rightarrow
\begin{cases}
	[\text{int\_literal}]\\
	[\text{identifier}] \\
\end{cases}


\end{align*}
$$