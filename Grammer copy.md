$$
\begin{align*}

\text{Program} &\to [\text{statement}]^* \\

[\text{Statement}] &\to
\begin{cases}
	\text{exit}([\text{Expression}]); \\
	\text{var}\space [\text{identifier}] = [\text{Expression}];\\
	[\text{identifier}] = [\text{Expression}];\\
	\text{if}([\text{Expression}]) [\text{statement}]\quad [\text{else if}([\text{Expression}])[statement]]^*\quad \text{else} [\text{statement}]^?\\
	[\text{Scope}]
\end{cases}\\

[\text{Scope}] &\to \{[\text{statement}]^*\}\\

[\text{Expression}] &\to
\begin{cases}
	[\text{Term}]\\
	[\text{Binary\_expression}] \\
\end{cases}\\

[\text{Binary\_expression}] &\to
\begin{cases}
	\text{precedence} = 0:\\
	\qquad[\text{Expression}] + [\text{Expression}]\\
	\qquad[\text{Expression}] - [\text{Expression}]\\
	\text{precedence} = 1:\\
	\qquad[\text{Expression}] * [\text{Expression}]\\
	\qquad[\text{Expression}]\space /\space [\text{Expression}]\\
\end{cases}\\

[\text{Term}] &\to
\begin{cases}
	[\text{int\_literal}]\\
	[\text{identifier}] \\
	(\text{Expression})
\end{cases}


\end{align*}
$$