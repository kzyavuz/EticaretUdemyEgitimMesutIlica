/* ===========================
       THEME TOGGLE
    =========================== */
const html = document.documentElement;
const themeBtn = document.getElementById("themeToggle");
const themeIcon = document.getElementById("themeIcon");

function applyTheme(theme) {
  html.setAttribute("data-theme", theme);
  themeIcon.className = theme === "dark" ? "bi bi-sun-fill" : "bi bi-moon-fill";
  localStorage.setItem("shopnova-theme", theme);
}

themeBtn.addEventListener("click", () => {
  const current = html.getAttribute("data-theme");
  applyTheme(current === "dark" ? "light" : "dark");
});

// Load saved theme
const saved = localStorage.getItem("shopnova-theme");
if (saved) applyTheme(saved);

/* ===========================
       COUNTDOWN TIMER
    =========================== */
let total = 8 * 3600 + 34 * 60 + 22;

function updateCountdown() {
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  document.getElementById("cd-h").textContent = String(h).padStart(2, "0");
  document.getElementById("cd-m").textContent = String(m).padStart(2, "0");
  document.getElementById("cd-s").textContent = String(s).padStart(2, "0");
  if (total > 0) total--;
}

updateCountdown();
setInterval(updateCountdown, 1000);

/* ===========================
       ADD TO CART
    =========================== */
let cartCount = 3;

function addToCart(btn) {
  cartCount++;
  document.getElementById("cartCount").textContent = cartCount;

  // Animate button
  btn.innerHTML = '<i class="bi bi-check-lg"></i> Eklendi!';
  btn.style.backgroundColor = "var(--clr-badge-new)";
  btn.style.borderColor = "var(--clr-badge-new)";
  btn.style.color = "#fff";

  setTimeout(() => {
    btn.innerHTML = '<i class="bi bi-bag-plus"></i> Sepete Ekle';
    btn.style.backgroundColor = "";
    btn.style.borderColor = "";
    btn.style.color = "";
  }, 1800);

  // Show toast
  const toast = new bootstrap.Toast(document.getElementById("cartToast"), {
    delay: 2500,
  });
  toast.show();
}

/* ===========================
       WISHLIST TOGGLE
    =========================== */
document.querySelectorAll(".wishlist-toggle").forEach((btn) => {
  btn.addEventListener("click", () => {
    const icon = btn.querySelector("i");
    if (icon.classList.contains("bi-heart")) {
      icon.className = "bi bi-heart-fill";
      btn.classList.add("wishlist-active");
    } else {
      icon.className = "bi bi-heart";
      btn.classList.remove("wishlist-active");
    }
  });
});

/* ===========================
       PRODUCT TABS
    =========================== */
document.querySelectorAll(".product-tabs .nav-link").forEach((tab) => {
  tab.addEventListener("click", function () {
    document
      .querySelectorAll(".product-tabs .nav-link")
      .forEach((t) => t.classList.remove("active"));
    this.classList.add("active");
  });
});

/* ===========================
       QTY CONTROL
    =========================== */
function changeQty(btn, delta) {
  const numEl = btn.parentElement.querySelector(".qty-num");
  let qty = parseInt(numEl.textContent) + delta;
  if (qty < 1) qty = 1;
  numEl.textContent = qty;
}

/* ===========================
       BACK TO TOP
    =========================== */
const backBtn = document.getElementById("backToTop");

window.addEventListener("scroll", () => {
  backBtn.classList.toggle("visible", window.scrollY > 300);
});

backBtn.addEventListener("click", () => {
  window.scrollTo({ top: 0, behavior: "smooth" });
});

/* ===========================
       COLOR SWATCHES
    =========================== */
document.querySelectorAll(".swatch").forEach((swatch) => {
  swatch.addEventListener("click", function () {
    this.closest(".color-swatches")
      .querySelectorAll(".swatch")
      .forEach((s) => s.classList.remove("active"));
    this.classList.add("active");
  });
});
