function editToFavorites(element, productId) {
    if (!element) return;

    // Favori ise çıkar, değilse ekle
    const isCurrentlyFav = element.getAttribute("data-favorite") === "true";
    const url = isCurrentlyFav
        ? element.getAttribute("data-remove-url")
        : element.getAttribute("data-add-url");

    // Antiforgery token'ı bul
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenElement) {
        console.error("Token bulunamadı!");
        return;
    }
    const token = tokenElement.value;

    element.disabled = true;

    fetch(url, {
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded",
            "RequestVerificationToken": token,
        },
        body: new URLSearchParams({
            productId: productId,
        })
    })
        .then((response) => {
            if (response.ok) {
                const nextState = !isCurrentlyFav;

                element.setAttribute("data-favorite", nextState ? "true" : "false");

                const solid = element.querySelector(".fa-solid.fa-heart");
                const regular = element.querySelector(".fa-regular.fa-heart");

                if (solid && regular) {
                    solid.classList.toggle("d-none", !nextState);
                    regular.classList.toggle("d-none", nextState);
                }

                if (isCurrentlyFav && window.location.pathname.toLowerCase().includes("favoriler")) {
                    let card = element.closest(".col-6, .col-md-4, .col-xl-3");
                    if (card) {
                        card.classList.add("fade-out");
                        setTimeout(() => card.remove(), 300);
                    }
                }
            } else {
                toastr.error("Bir sorun oluştu. Lütfen tekrar deneyin.");
            }
        })
        .catch((error) => {
            console.error("Hata:", error);
        })
        .finally(() => {
            element.disabled = false;
        });
}

function editToCart(element, productId) {
    if (!element) return;

    const isAdd = element.getAttribute("data-process-type") === "add";

    const url = isAdd
        ? element.getAttribute("data-add-url")
        : element.getAttribute("data-remove-url");

    // Antiforgery token'ı bul
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenElement) {
        console.error("Token bulunamadı!");
        return;
    }
    const token = tokenElement.value;

    element.disabled = true;

    const row = element.closest("tr");
    let quantity = 1;
    if (row) {
        const qtyInp = row.querySelector(".quantity-input");
        quantity = qtyInp ? qtyInp.value : 1;
    }

    fetch(url, {
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded",
            "RequestVerificationToken": token,
        },
        body: new URLSearchParams({
            productId: productId,
            quantity: quantity
        })
    })
        .then(async (response) => {
            if (response.ok) {

                if (!isAdd) {
                    // Silme efekti
                    row.style.transition = "opacity 0.3s";
                    row.style.opacity = 0;
                    toastr.warning(`Ürün sepetten çıkarıldı!`);
                    setTimeout(() => { row.remove(); updateSummary(); }, 300);
                } else {

                    element.classList.add("btn-in-cart");

                    const icon = element.querySelector("i");
                    if (icon) {
                        icon.classList.remove("fa-shopping-cart");
                        icon.classList.add("fa-check", "me-1");
                    }

                    const textSpan = element.querySelector(".btn-text");
                    if (textSpan) {
                        textSpan.textContent = "Sepette";
                    }
                    updateSummary();
                }
            }
        })
        .finally(() => { element.disabled = false; });
}

// Yeni: Sepet özetini güncelleyen yardımcı fonksiyon
function updateSummary() {
    const summaryUrl = document.getElementById("cart-content").dataset.summaryUrl;
    fetch(summaryUrl)
        .then(r => r.json())
        .then(data => {
            document.getElementById("summary-subtotal").textContent = data.subTotal;
            document.getElementById("summary-tax").textContent = data.tax;
            document.getElementById("summary-shipping").textContent = data.shipping;
            document.getElementById("summary-total").textContent = data.total;

            // Eğer tablo boşsa sayfayı yenile veya mesajı göster
            if (document.querySelectorAll("tbody tr").length === 0) {
                location.reload();
            }
        });
}

// Yeni: Artı/Eksi butonları için fonksiyon
function changeQuantity(btn, productId, change) {
    const input = btn.parentElement.querySelector(".quantity-input");
    const row = btn.closest("tr");
    let currentQty = parseInt(input.value);
    let newQty = currentQty + change;

    // Eğer miktar 1'den 0'a düşerse (Eksiye basıldı ve şu an 1 ise)
    if (newQty <= 0) {
        const deleteBtn = row.querySelector('button[data-process-type="remove"]');
        editToCart(deleteBtn, productId);
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    const url = btn.getAttribute("data-edit-url");

    fetch(url, {
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded",
            "RequestVerificationToken": token,
        },
        body: new URLSearchParams({
            productId: productId,
            quantity: newQty
        })
    })
        .then(response => {
            if (response.ok) {
                input.value = newQty;

                // Satır bazlı toplam fiyatı güncelle (Opsiyonel ama şık durur)
                // Birim fiyatı çekip (TL simgesini temizleyerek) adetle çarpın
                const priceText = row.querySelector("td:nth-child(4)").textContent;
                const unitPrice = parseFloat(priceText.replace(/\./g, '').replace(',', '.').replace(/[^0-9.]/g, ''));
                const totalCell = row.querySelector("td:nth-child(5)");
                totalCell.textContent = (unitPrice * newQty).toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' });

                // Ana sepet özetini güncelle
                updateSummary();
                toastr.success("Miktar güncellendi.");
            } else {
                toastr.error("Miktar güncellenemedi!");
            }
        })
        .catch(error => {
            console.error("Hata:", error);
        });
}