document.addEventListener("DOMContentLoaded", () => {
    const messagesBox = document.getElementById("messages");
    const solicitudInput = document.querySelector(
        '.composer input[name="solicitudId"]'
    );
    const composer = document.querySelector(".composer");
    const messageInput = composer?.querySelector(
        'input[name="mensaje"]'
    );
    const attachmentInput = composer?.querySelector(
        '.attach input[type="file"]'
    );

    if (!messagesBox || !solicitudInput || !composer || !messageInput) {
        return;
    }

    const solicitudId = solicitudInput.value;
    const currentUserId = Number(
        messagesBox.dataset.currentUserId || 0
    );

    let polling = false;

    function valueOf(object, lower, upper) {
        return object?.[lower] ?? object?.[upper];
    }

    function renderMessages(data) {
        const wasNearBottom =
            messagesBox.scrollTop +
            messagesBox.clientHeight >=
            messagesBox.scrollHeight - 40;

        const fragment = document.createDocumentFragment();

        data.forEach(message => {
            const remitenteId = Number(
                valueOf(message, "remitenteId", "RemitenteId") || 0
            );

            const remitente =
                valueOf(message, "remitente", "Remitente") || "";

            const texto =
                valueOf(message, "mensaje", "Mensaje") || "";

            const creadoEn =
                valueOf(message, "creadoEn", "CreadoEn");

            const adjunto =
                valueOf(message, "adjunto", "Adjunto");

            const div = document.createElement("div");

            div.className =
                "message" +
                (remitenteId === currentUserId
                    ? " mine"
                    : "");

            const author = document.createElement("b");
            author.textContent = remitente;

            const time = document.createElement("small");

            if (creadoEn) {
                time.textContent =
                    new Date(creadoEn).toLocaleTimeString(
                        [],
                        {
                            hour: "2-digit",
                            minute: "2-digit"
                        }
                    );
            }

            div.appendChild(author);
            div.appendChild(time);

            if (texto.trim() !== "") {
                const p = document.createElement("p");
                p.textContent = texto;
                div.appendChild(p);
            }

            if (adjunto) {
                const wrapper =
                    document.createElement("div");

                wrapper.className =
                    "message-attachment";

                const link =
                    document.createElement("a");

                const attachmentId =
                    Number(
                        valueOf(
                            adjunto,
                            "id",
                            "Id"
                        )
                    );

                const originalName =
                    valueOf(
                        adjunto,
                        "nombreOriginal",
                        "NombreOriginal"
                    ) || "Archivo adjunto";

                link.href =
                    "/Archivo/Download/" +
                    attachmentId;

                link.textContent =
                    "📎 " + originalName;

                link.target = "_blank";
                link.rel = "noopener";

                wrapper.appendChild(link);
                div.appendChild(wrapper);
            }

            fragment.appendChild(div);
        });

        messagesBox.replaceChildren(fragment);

        if (wasNearBottom) {
            messagesBox.scrollTop =
                messagesBox.scrollHeight;
        }
    }

    async function updateMessages() {
        if (polling) {
            return;
        }

        polling = true;

        try {
            const response = await fetch(
                "/Chat/MessagesJson/" + encodeURIComponent(solicitudId),
                {
                    method: "GET",
                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    },
                    cache: "no-store"
                }
            );

            if (!response.ok) {
                return;
            }

            const data = await response.json();

            if (Array.isArray(data)) {
                renderMessages(data);
            }
        } catch (error) {
            // El chat continúa funcionando aunque una consulta
            // de actualización falle temporalmente.
        } finally {
            polling = false;
        }
    }

    setInterval(updateMessages, 2500);

    if (attachmentInput) {
        attachmentInput.addEventListener("change", () => {
            const file =
                attachmentInput.files?.[0];

            const attachLabel =
                composer.querySelector(".attach");

            if (!attachLabel) {
                return;
            }

            attachLabel.title =
                file
                    ? file.name
                    : "Adjuntar archivo";
        });
    }

    const emojiButton =
        document.createElement("button");

    emojiButton.type = "button";
    emojiButton.className = "emoji-btn";
    emojiButton.textContent = "☺";
    emojiButton.title = "Insertar emoji";

    const emojis = [
        "😀", "🙂", "😂", "👍",
        "🙏", "👏", "💻", "🛠️",
        "📌", "✅", "⚠️", "❤️"
    ];

    emojiButton.addEventListener("click", () => {
        const existing =
            document.querySelector(".emoji-pop");

        if (existing) {
            existing.remove();
            return;
        }

        const popup =
            document.createElement("div");

        popup.className = "emoji-pop";

        emojis.forEach(emoji => {
            const button =
                document.createElement("button");

            button.type = "button";
            button.textContent = emoji;

            button.addEventListener("click", () => {
                const start =
                    messageInput.selectionStart ??
                    messageInput.value.length;

                const end =
                    messageInput.selectionEnd ??
                    messageInput.value.length;

                messageInput.value =
                    messageInput.value.slice(0, start) +
                    emoji +
                    messageInput.value.slice(end);

                messageInput.focus();

                const position =
                    start + emoji.length;

                messageInput.setSelectionRange(
                    position,
                    position
                );

                popup.remove();
            });

            popup.appendChild(button);
        });

        document.body.appendChild(popup);

        const rect =
            emojiButton.getBoundingClientRect();

        popup.style.left =
            Math.max(8, rect.left) + "px";

        popup.style.top =
            Math.max(
                8,
                rect.top - popup.offsetHeight - 8
            ) + "px";
    });

    const attachmentLabel =
        composer.querySelector(".attach");

    composer.insertBefore(
        emojiButton,
        attachmentLabel
    );
});
