
// MOBIL MENU KONTROLLERİ
// ============================================
var mobOpen = false;

// ============================================
// MEGA MENU & KATEGORİ PANEL KONTROLLERİ
// ============================================
document.addEventListener("DOMContentLoaded", function () {
    // ── Mega Menu Hover Toggle ──
    var megaItems = document.querySelectorAll("li.has-mega");
    var megaCloseTimer = null;

    megaItems.forEach(function (li) {
        var mega = li.querySelector(".nav-mega");
        var trigger = li.querySelector(":scope > a");
        if (!mega) return;

        li.addEventListener("mouseenter", function () {
            if (megaCloseTimer) {
                clearTimeout(megaCloseTimer);
                megaCloseTimer = null;
            }
            megaItems.forEach(function (other) {
                if (other !== li) {
                    var otherMega = other.querySelector(".nav-mega");
                    var otherTrigger = other.querySelector(":scope > a");
                    if (otherMega) otherMega.classList.remove("is-active");
                    if (otherTrigger) otherTrigger.setAttribute("aria-expanded", "false");
                }
            });
            mega.classList.add("is-active");
            if (trigger) trigger.setAttribute("aria-expanded", "true");
        });

        li.addEventListener("mouseleave", function () {
            megaCloseTimer = setTimeout(function () {
                mega.classList.remove("is-active");
                if (trigger) trigger.setAttribute("aria-expanded", "false");
            }, 120);
        });

        mega.addEventListener("mouseenter", function () {
            if (megaCloseTimer) {
                clearTimeout(megaCloseTimer);
                megaCloseTimer = null;
            }
        });

        mega.addEventListener("mouseleave", function () {
            megaCloseTimer = setTimeout(function () {
                mega.classList.remove("is-active");
                if (trigger) trigger.setAttribute("aria-expanded", "false");
            }, 120);
        });
    });

    // Herhangi bir yere tıklandığında mega menüleri kapat
    document.addEventListener("click", function (e) {
        if (!e.target.closest("li.has-mega")) {
            megaItems.forEach(function (li) {
                var mega = li.querySelector(".nav-mega");
                var trigger = li.querySelector(":scope > a");
                if (mega) mega.classList.remove("is-active");
                if (trigger) trigger.setAttribute("aria-expanded", "false");
            });
        }
    });

    // ── Kategori Kenar Çubuğu Hover (Trendyol-style) ──
    var catSidebarItems = document.querySelectorAll(".cat-sidebar-item");

    catSidebarItems.forEach(function (item) {
        item.addEventListener("mouseenter", function () {
            var catId = this.dataset.catId;

            catSidebarItems.forEach(function (i) {
                i.classList.remove("is-active");
            });
            this.classList.add("is-active");

            document.querySelectorAll(".cat-sub-panel").forEach(function (panel) {
                panel.classList.remove("is-active");
            });

            var activePanel = document.querySelector(
                ".cat-sub-panel[data-parent-id='" + catId + "']",
            );
            if (activePanel) {
                activePanel.classList.add("is-active");
            }
        });
    });
});

// Mobil menüyü açma/kapatma işlemi
function toggleMobileMenu() {
    mobOpen = !mobOpen;
    document.getElementById("mobileDrawer").classList.toggle("open", mobOpen);
    var h = document.getElementById("hamburger");
    h.classList.toggle("open", mobOpen);
    h.setAttribute("aria-expanded", mobOpen);
    var s = document.getElementById("mobScrim");
    if (s) s.classList.toggle("open", mobOpen);
    // Scroll engellemesi: mobil menü açıkken sayfa kaydırma engellenir
    if (mobOpen) {
        document.body.classList.add("no-scroll");
    } else {
        document.body.classList.remove("no-scroll");
    }
    document.body.style.overflow = mobOpen ? "hidden" : "";
}

// Mobil menüyü kapatma fonksiyonu
function closeMob() {
    if (!mobOpen) return;
    mobOpen = false;
    document.getElementById("mobileDrawer").classList.remove("open");
    var h = document.getElementById("hamburger");
    h.classList.remove("open");
    h.setAttribute("aria-expanded", "false");
    var s = document.getElementById("mobScrim");
    if (s) s.classList.remove("open");
    document.body.classList.remove("no-scroll");
    document.body.style.overflow = "";
}

// Alt menü aç kapa işlemi (mobil menüdeki açılır menüler)
function toggleMobSub(id, btnId) {
    var sub = document.getElementById(id);
    var btn = document.getElementById(btnId);
    var isOpen = sub.classList.contains("open");
    document.querySelectorAll(".mob-sub").forEach(function (s) {
        s.classList.remove("open");
    });
    document.querySelectorAll(".mob-link").forEach(function (l) {
        l.classList.remove("sub-open");
    });
    if (!isOpen) {
        sub.classList.add("open");
        if (btn) {
            btn.classList.add("sub-open");
            btn.setAttribute("aria-expanded", "true");
        }
    }
}

// ============================================
// SEPET PANELİ KONTROLLERİ
// ============================================
function toggleCartPanel() {
    var panel = document.getElementById("cartPanel");
    var scrim = document.getElementById("cartScrim");
    panel.classList.toggle("open");
    scrim.classList.toggle("open");
    if (panel.classList.contains("open")) {
        document.body.classList.add("no-scroll");
    } else {
        document.body.classList.remove("no-scroll");
    }
}

// ============================================
// AYARLAR (THEME, RADIUS, YAZITAŞI)
// ============================================

// Ayarlar panelini açma/kapatma
function toggleSettings() {
    var panel = document.getElementById("settingsPanel");
    panel.classList.toggle("open");
    document.getElementById("settingsScrim").classList.toggle("open");
    if (panel.classList.contains("open")) {
        document.body.classList.add("no-scroll");
    } else {
        document.body.classList.remove("no-scroll");
    }
}

// Tema değiştirme (dark/light)
function setTheme(t, el) {
    // 1. Tüm geçiş efektlerini geçici olarak kapat
    document.documentElement.classList.add("disable-transitions");

    // 2. Temayı set et
    document.documentElement.setAttribute("data-theme", t);
    document.documentElement.setAttribute("data-bs-theme", t);

    // UI güncellemeleri
    document.querySelectorAll(".theme-tile").forEach(function (x) {
        x.classList.remove("on");
        x.setAttribute("aria-checked", "false");
    });

    if (el) {
        el.classList.add("on");
        el.setAttribute("aria-checked", "true");
    }

    localStorage.setItem("ap2-theme", t);

    // 3. Çok kısa bir süre sonra animasyonları tekrar aç
    // requestAnimationFrame tarayıcının bir sonraki boyama işlemini beklemesini sağlar
    requestAnimationFrame(function () {
        requestAnimationFrame(function () {
            document.documentElement.classList.remove("disable-transitions");
        });
    });
}

// Köşe yuvarlama oranı değiştirme
function setRadius(r, el) {
    document.documentElement.classList.add("disable-transitions");

    document.documentElement.setAttribute("data-radius", r);
    document.querySelectorAll(".sp-pill[data-r]").forEach(function (x) {
        x.classList.remove("on");
    });
    if (el) el.classList.add("on");
    localStorage.setItem("ap2-radius", r);

    requestAnimationFrame(function () {
        document.documentElement.classList.remove("disable-transitions");
    });
}

// Yazı tipi değiştirme (poppins, roboto vb)
function setFont(f, el) {
    document.documentElement.setAttribute("data-font", f);
    document.querySelectorAll(".sp-font-btn").forEach(function (x) {
        x.classList.remove("on");
    });
    if (el) el.classList.add("on");
    localStorage.setItem("ap2-font", f);
}

// Filtre Alanı Görünümü (drawer/sidebar)
function setFilterMode(m, el) {
    document.documentElement.setAttribute("data-filter-mode", m);
    document.querySelectorAll(".sp-pill[data-fm]").forEach(function (x) {
        x.classList.remove("on");
    });
    if (el) el.classList.add("on");
    localStorage.setItem("ap2-filtermode", m);
}

// ============================================
// DİL AYARLARI
// ============================================

// Dil değiştirme (localStorage'da saklama)
function setLang(code, el) {
    var flags = { tr: "🇹🇷", en: "🇬🇧", de: "🇩🇪", ar: "🇸🇦" };
    var codes = { tr: "TR", en: "EN", de: "DE", ar: "AR" };
    var btn = document.getElementById("langBtn");
    if (btn)
        btn.innerHTML =
            '<span class="lang-flag" aria-hidden="true">' +
            flags[code] +
            '</span><span id="langCode">' +
            codes[code] +
            '</span><i class="fa fa-chevron-down" style="font-size:9px" aria-hidden="true"></i>';
    localStorage.setItem("ap2-lang", code);
}

// Dil seçimi (settings panel özel dropdown)
function pickLang(optEl) {
    var code = optEl.dataset.value;
    var flag = optEl.dataset.flag;
    var name = optEl.dataset.name;
    // Trigger güncelle
    var tiFlag = document.getElementById("spLangTiFlag");
    var tiName = document.getElementById("spLangTiName");
    if (tiFlag) tiFlag.textContent = flag;
    if (tiName) tiName.textContent = name;
    // Aktif séçeneği işaretle
    document.querySelectorAll(".sp-lang-opt").forEach(function (el) {
        el.classList.toggle("active", el === optEl);
    });
    // Kapat
    var dd = document.getElementById("spLangDd");
    if (dd) {
        dd.classList.remove("is-open");
        var trigger = document.getElementById("spLangTrigger");
        if (trigger) trigger.setAttribute("aria-expanded", "false");
    }
    localStorage.setItem("ap2-lang", code);
}

// Dil dropdown toggle
function toggleLangDd() {
    var dd = document.getElementById("spLangDd");
    if (!dd) return;
    var open = dd.classList.toggle("is-open");
    var trigger = document.getElementById("spLangTrigger");
    if (trigger) trigger.setAttribute("aria-expanded", open ? "true" : "false");
}

// Ayarları sıfırlama (varsayılan değerlere dönüş)
function resetSettings() {
    setTheme("dark", document.querySelector('[data-tile="dark"]'));
    setRadius("sharp", document.querySelector('[data-r="sharp"]'));
    setFont("poppins", document.querySelector('[data-font="poppins"]'));
    setFilterMode("sidebar", document.querySelector('[data-fm="sidebar"]'));
}

// Kayıtlı ayarları localStorage'dan geri yükleme
function restoreSettings() {
    var t = localStorage.getItem("ap2-theme");
    var r = localStorage.getItem("ap2-radius");
    var fn = localStorage.getItem("ap2-font");
    var fm = localStorage.getItem("ap2-filtermode");
    if (t) {
        document.documentElement.setAttribute("data-theme", t);
        document.documentElement.setAttribute("data-bs-theme", t); // Bootstrap 5.3+ desteği
        document.querySelectorAll(".theme-tile").forEach(function (el) {
            el.classList.remove("on");
            el.setAttribute("aria-checked", "false");
        });
        var tEl = document.querySelector('[data-tile="' + t + '"]');
        if (tEl) {
            tEl.classList.add("on");
            tEl.setAttribute("aria-checked", "true");
        }
    }
    if (r) {
        document.documentElement.setAttribute("data-radius", r);
        document.querySelectorAll(".sp-pill[data-r]").forEach(function (el) {
            el.classList.toggle("on", el.dataset.r === r);
        });
    }
    if (fn) {
        document.documentElement.setAttribute("data-font", fn);
        document.querySelectorAll(".sp-font-btn").forEach(function (el) {
            el.classList.toggle("on", el.dataset.font === fn);
        });
    }
    if (fm) {
        document.documentElement.setAttribute("data-filter-mode", fm);
        document.querySelectorAll(".sp-pill[data-fm]").forEach(function (el) {
            el.classList.toggle("on", el.dataset.fm === fm);
        });
    } else {
        // Varsayılan olarak sidebar
        document.documentElement.setAttribute("data-filter-mode", "sidebar");
        var defaultFmEl = document.querySelector('[data-fm="sidebar"]');
        if (defaultFmEl) defaultFmEl.classList.add("on");
    }
    // Dil seçimini geri yükle
    var lang = localStorage.getItem("ap2-lang");
    if (lang) {
        var flags = { tr: "🇹🇷", en: "🇬🇧", de: "🇩🇪", ar: "🇸🇦" };
        var names = { tr: "Türkçe", en: "İngilizce", de: "Almanca", ar: "Arapça" };
        var tiFlag = document.getElementById("spLangTiFlag");
        var tiName = document.getElementById("spLangTiName");
        if (tiFlag) tiFlag.textContent = flags[lang] || "";
        if (tiName) tiName.textContent = names[lang] || "";
        document.querySelectorAll(".sp-lang-opt").forEach(function (el) {
            el.classList.toggle("active", el.dataset.value === lang);
        });
    }
}

// ============================================
// ÜRÜN DETAY SAYFASI FONKSİYONLARI
// ============================================

// Ana resimleri değiştirme (thumbnail tıkladığında)
function swapImg(el, src, idx) {
    document.getElementById("mainImg").src = src;
    document.getElementById("mainImg").alt =
        "Alüminyum Fitilleri görsel " + (idx + 1);
    document.querySelectorAll(".adv-thumb").forEach(function (t) {
        t.classList.remove("on");
    });
    el.classList.add("on");
    lbCurrent = idx || 0;
}

// Ürün miktarını değiştirme (artırma/azaltma)
function chQty(d) {
    var inp = document.getElementById("qtyInp");
    var min = parseInt(inp.min) || 1;
    inp.value = Math.max(min, (parseInt(inp.value) || min) + d);
}

// Detay sayfasında tab açma (açıklama/teknik özellikleri gösterme)
function detTab(btn, id) {
    document.querySelectorAll(".det-tab").forEach(function (t) {
        t.classList.remove("on");
        t.setAttribute("aria-selected", "false");
    });
    btn.classList.add("on");
    btn.setAttribute("aria-selected", "true");
    document.getElementById("dt-desc").style.display =
        id === "dt-desc" ? "block" : "none";
    document.getElementById("dt-tech").style.display =
        id === "dt-tech" ? "block" : "none";
}

// ============================================
// ARAMA OVERLAY
// ============================================

// Arama panelini açma
function openSearch() {
    document.getElementById("searchOverlay").classList.add("open");
    document.body.classList.add("no-scroll");
    document.body.style.overflow = "hidden";
    setTimeout(function () {
        document.getElementById("searchInp").focus();
    }, 100);
}

// Arama panelini kapatma
function closeSearch() {
    document.getElementById("searchOverlay").classList.remove("open");
    document.body.classList.remove("no-scroll");
    document.body.style.overflow = "";
}

// ============================================
// FİLTRE DRAWER (ÜRÜN FİLTRELEME)
// ============================================

// Filtre panelini açma
function openFilter() {
    document.getElementById("filterDrawer").classList.add("open");
    document.getElementById("filterScrim").classList.add("open");
    document.body.classList.add("no-scroll");
    document.body.style.overflow = "hidden";
}
function closeFilter() {
    document.getElementById("filterDrawer").classList.remove("open");
    document.getElementById("filterScrim").classList.remove("open");
    document.body.classList.remove("no-scroll");
    document.body.style.overflow = "";
}

// Filtre materyali butonları - seçim toggle işlemi (sayfa yüklendikten sonra bağlanır)
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".fd-mat-btn").forEach(function (btn) {
        btn.addEventListener("click", function () {
            this.classList.toggle("on");
        });
    });
});

// ============================================
// LIGHTBOX (BÜYÜK RESİM GÖSTERICI)
// ============================================

// Lightbox gösterilecek resimlerin listesi
var lbImgs = [
    "/images/product/7dc7fd47-110d-42e3-bb41-fea06be07f51.webp",
    "/images/product/360bc25d-34fa-415c-a03a-9bc41f8ccff4.webp",
    "/images/product/ec09db84-a4c0-4f72-aca8-f62969b0a9d9.jpg",
];
var lbCurrent = 0;

// Lightbox açma
function openLightbox(idx) {
    lbCurrent = idx || 0;
    document.getElementById("lbImg").src = lbImgs[lbCurrent];
    document.getElementById("lbImg").alt =
        "Ürün görseli " + (lbCurrent + 1) + " / " + lbImgs.length;
    document.getElementById("lbCounter").textContent =
        lbCurrent + 1 + " / " + lbImgs.length;
    document.getElementById("imgLightbox").classList.add("open");
    document.body.style.overflow = "hidden";
}

// Lightbox kapatma
function closeLightbox() {
    document.getElementById("imgLightbox").classList.remove("open");
    document.body.style.overflow = "";
}

// Lightbox dış alanına tıklandığında kapatma
function lbClose(e) {
    if (e.target === e.currentTarget) closeLightbox();
}

// Lightbox'ta resimleri gezme (önceki/sonraki)
function lbNav(dir) {
    lbCurrent = (lbCurrent + dir + lbImgs.length) % lbImgs.length;
    var img = document.getElementById("lbImg");
    img.style.opacity = "0";
    setTimeout(function () {
        img.src = lbImgs[lbCurrent];
        img.alt = "Ürün görseli " + (lbCurrent + 1) + " / " + lbImgs.length;
        img.style.opacity = "1";
        document.getElementById("lbCounter").textContent =
            lbCurrent + 1 + " / " + lbImgs.length;
    }, 150);
}

// ============================================
// GALERİ FİLTRELEME
// ============================================

// Galeride kategori filtresi uygulama
function gFilter(cat, btn) {
    document.querySelectorAll(".g-tab").forEach(function (b) {
        b.classList.remove("on");
        b.setAttribute("aria-selected", "false");
    });
    btn.classList.add("on");
    btn.setAttribute("aria-selected", "true");

    var items = document.querySelectorAll("#galleryGrid .gallery-item");

    // Önce tüm görünür olanları fade-out yap
    items.forEach(function (item) {
        item.style.transition = "opacity .25s ease, transform .25s ease";
        item.style.opacity = "0";
        item.style.transform = "scale(0.95)";
    });

    // Fade-out bittikten sonra gizle/göster ve fade-in yap
    setTimeout(function () {
        items.forEach(function (item) {
            var match = cat === "all" || item.dataset.cat === cat;
            item.style.display = match ? "" : "none";
            if (match) {
                // Küçük gecikmeyle fade-in
                setTimeout(function () {
                    item.style.transition = "opacity .35s ease, transform .35s ease";
                    item.style.opacity = "1";
                    item.style.transform = "scale(1)";
                }, 30);
            }
        });
    }, 260);
}

/* ── Yorum gönder ── */
function submitComment(e) {
    e.preventDefault();
    var btn = e.target.querySelector("button[type='submit']");
    btn.innerHTML =
        '<i class="fa fa-spinner fa-spin" aria-hidden="true"></i> Gönderiliyor...';
    btn.disabled = true;
    setTimeout(function () {
        btn.innerHTML =
            '<i class="fa fa-check" aria-hidden="true"></i> Yorumunuz Alındı!';
        btn.style.background = "#22c55e";
    }, 1500);
}

/* ── Bülten ── */
function subscribeNl() {
    var inputs = document.querySelectorAll(".newsletter-inp");
    inputs.forEach(function (inp) {
        if (!inp.value) return;
        var btn = inp.closest(".newsletter-body").querySelector(".btn-p");
        btn.innerHTML =
            '<i class="fa fa-check" aria-hidden="true"></i> Abone Oldunuz!';
        btn.style.background = "#22c55e";
        btn.disabled = true;
    });
}

// Blog sayfası gösterme — blog_detail.php'deki "Tüm Makaleler" butonları için
function showPage(page) {
    if (page === "list") {
        window.location.href = "blog.php";
    }
}

// Görünüm tipini değiştirme (grid/list/table)
function setView(type, containerId, btnPrefix) {
    var container = document.getElementById(containerId);
    if (!container) return;

    // Sınıfları temizle ve yenisini ekle
    container.classList.remove("view-grid", "view-list", "view-table");
    container.classList.add("view-" + type);

    // Buton durumlarını güncelle
    if (btnPrefix) {
        var gridBtn = document.getElementById(btnPrefix + "Grid");
        var listBtn = document.getElementById(btnPrefix + "List");
        var tableBtn = document.getElementById(btnPrefix + "Table");

        if (gridBtn) {
            gridBtn.classList.toggle("active", type === "grid");
            gridBtn.setAttribute("aria-pressed", type === "grid");
        }
        if (listBtn) {
            listBtn.classList.toggle("active", type === "list");
            listBtn.setAttribute("aria-pressed", type === "list");
        }
        if (tableBtn) {
            tableBtn.classList.toggle("active", type === "table");
            tableBtn.setAttribute("aria-pressed", type === "table");
        }
    }

    // Animasyonu tetikle
    container.style.opacity = "0";
    container.style.transform = "translateY(10px)";
    setTimeout(function () {
        container.style.transition = "opacity 0.4s ease, transform 0.4s ease";
        container.style.opacity = "1";
        container.style.transform = "translateY(0)";
    }, 50);

    localStorage.setItem("view-" + containerId, type);
}

// Elemanları sıralama (A-Z, Z-A, En Yeni vb.)
function sortItems(criteria, containerId, itemSelector, attrSelector) {
    var container = document.getElementById(containerId);
    if (!container) return;

    var items = Array.prototype.slice.call(
        container.querySelectorAll(itemSelector),
    );

    items.sort(function (a, b) {
        var valA = a.querySelector(attrSelector).textContent.trim().toLowerCase();
        var valB = b.querySelector(attrSelector).textContent.trim().toLowerCase();

        if (criteria === "az") {
            return valA.localeCompare(valB, "tr");
        } else if (criteria === "za") {
            return valB.localeCompare(valA, "tr");
        }
        return 0;
    });

    // Container'ı temizle ve sıralanmış item'ları ekle
    items.forEach(function (item) {
        container.appendChild(item);
    });

    // Animasyon
    container.style.opacity = "0";
    setTimeout(function () {
        container.style.opacity = "1";
    }, 100);
}

// ============================================
// ARAMA FONKSİYONU (Canlı Arama & Premium Tasarım)
// ============================================

var searchData = null; // Ürünler ve blog verileri buraya yüklenecek

// Arama overlay'ından arama yapma
function doSearch() {
    var inp = document.getElementById("searchInp");
    if (!inp) return;
    var val = inp.value.trim();
    if (val) {
        window.location.href =
            "../page/product.php?search=" + encodeURIComponent(val);
    }
}

// Canlı aramayı başlatma (verileri fetch etme)
function initLiveSearch() {
    var inp = document.getElementById("searchInp");
    var resultsGrid = document.getElementById("searchResults");
    var resultsCont = document.getElementById("searchResultsContainer");
    var noResults = document.getElementById("searchNoResults");
    var loading = document.getElementById("searchLoading");

    if (!inp) return;

    inp.addEventListener("input", function () {
        var query = this.value.trim().toLowerCase();

        // En az 2 karakter girilince aramaya başla
        if (query.length < 2) {
            resultsCont.style.display = "none";
            return;
        }

        // Veriler henüz yüklenmemişse yükle
        if (!searchData) {
            loading.style.display = "block";
            fetch("../data/search_data.php")
                .then((res) => res.json())
                .then((data) => {
                    searchData = data;
                    loading.style.display = "none";
                    performSearch(query);
                });
        } else {
            performSearch(query);
        }
    });

    function performSearch(q) {
        var matches = [];

        // Ürünlerde ara
        if (searchData.products) {
            searchData.products.forEach((p) => {
                if (
                    p.title.toLowerCase().includes(q) ||
                    p.desc.toLowerCase().includes(q) ||
                    p.cat.toLowerCase().includes(q)
                ) {
                    matches.push({ ...p, type: "Ürün" });
                }
            });
        }

        // Bloglarda ara
        if (searchData.blogs) {
            searchData.blogs.forEach((b) => {
                if (
                    b.title.toLowerCase().includes(q) ||
                    b.cat.toLowerCase().includes(q)
                ) {
                    matches.push({ ...b, type: "Blog" });
                }
            });
        }

        renderResults(matches);
    }

    function renderResults(list) {
        resultsGrid.innerHTML = "";
        if (list.length > 0) {
            list.forEach((item) => {
                var html = `
          <a href="${item.url}" class="search-res-item">
            ${item.img ? `<img src="${item.img}" class="search-res-img" alt="">` : ""}
            <div class="search-res-info">
              <span class="search-res-title">${item.title}</span>
              <span class="search-res-cat">${item.cat}</span>
            </div>
            <span class="search-res-type">${item.type}</span>
          </a>
        `;
                resultsGrid.insertAdjacentHTML("beforeend", html);
            });
            resultsCont.style.display = "block";
            noResults.style.display = "none";
        } else {
            resultsCont.style.display = "block";
            noResults.style.display = "block";
        }
    }
}

/* ── Bağlantı kopyala ── */
function copyLink() {
    navigator.clipboard &&
        navigator.clipboard.writeText(window.location.href).then(function () {
            var btn = document.querySelector(".blog-share-btn.cp");
            var orig = btn.innerHTML;
            btn.innerHTML =
                '<i class="fa fa-check" aria-hidden="true"></i> Kopyalandı!';
            setTimeout(function () {
                btn.innerHTML = orig;
            }, 2000);
        });
}

// ============================================
// GLOBAL TUŞLAŞTI (KEYBOARD) KONTROLLERI
// ============================================

// Escape tuşu ile panelleri kapatma, ok tuşları ile lightbox kontrolü
document.addEventListener("keydown", function (e) {
    if (e.key === "Escape") {
        var so = document.getElementById("searchOverlay");
        if (so) closeSearch();
        var lb2 = document.getElementById("imgLightbox");
        if (lb2) closeLightbox();
    }
    var lb = document.getElementById("imgLightbox");
    if (lb && lb.classList.contains("open")) {
        if (e.key === "ArrowLeft") lbNav(-1);
        if (e.key === "ArrowRight") lbNav(1);
    }
});

// ============================================
// SAYFA BAŞLATILDIĞINDA (DOM READY)
// ============================================

// AOS (Görünüme Girince Animasyon), Swiper ve ayarları başlatılması
document.addEventListener("DOMContentLoaded", function () {
    // Bootstrap tooltip başlatılması
    var tooltipElements = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltipElements.forEach(function (el) {
        new bootstrap.Tooltip(el);
    });

    // AOS başlatılması - scroll animasyonları
    if (typeof AOS !== "undefined") {
        AOS.init({
            duration: 650,
            easing: "ease-out-cubic",
            once: true,
            offset: 50,
        });
    }

    // Swiper slider başlatılması (carousel/kaydırılır bölümler)
    if (typeof Swiper !== "undefined") {
        // Hero bölümü slider
        new Swiper(".hero-swiper", {
            loop: true,
            effect: "fade",
            fadeEffect: { crossFade: true },
            autoplay: { delay: 3800, disableOnInteraction: false },
            pagination: {
                el: ".hero-swiper .swiper-pagination",
                clickable: true,
            },
            a11y: {
                enabled: true,
                prevSlideMessage: "Önceki slayt",
                nextSlideMessage: "Sonraki slayt",
            },
        });

        // Anasayfa ürünler slider
        new Swiper(".products-home-swiper", {
            slidesPerView: 1.15,
            slidesPerGroup: 1,
            spaceBetween: 14,
            pagination: {
                el: ".products-home-swiper .swiper-pagination",
                clickable: true,
            },
            breakpoints: {
                480: { slidesPerView: 1.6, slidesPerGroup: 1, spaceBetween: 16 },
                768: { slidesPerView: 3, slidesPerGroup: 3, spaceBetween: 20 },
                1200: { slidesPerView: 4, slidesPerGroup: 4, spaceBetween: 24 },
            },
            a11y: { enabled: true },
        });

        // Neden Biz? bölümü slider
        new Swiper(".why-swiper", {
            slidesPerView: 1.15,
            spaceBetween: 14,
            pagination: {
                el: ".why-swiper .swiper-pagination",
                clickable: true,
            },
            breakpoints: {
                480: { slidesPerView: 1.6, spaceBetween: 16 },
                768: { slidesPerView: 3, spaceBetween: 20 },
                1200: { slidesPerView: 4, spaceBetween: 24 },
            },
            a11y: { enabled: true },
        });

        // Sertifikalar slider
        new Swiper(".cert-swiper", {
            slidesPerView: 1,
            spaceBetween: 14,
            pagination: {
                el: ".cert-swiper .swiper-pagination",
                clickable: true,
            },
            breakpoints: { 480: { slidesPerView: 1.5 } },
            a11y: { enabled: true },
        });
    }

    // Kaydedilen kullanıcı ayarlarını geri yükleme (tema, yazı tipi vb)
    // Not: data-* attribute'ları head.php inline scripti ile sayfa yüklenmeden önce set edilir.
    // restoreSettings() burada sadece settings panelinin UI durumunu (checkbox/active class) senkronize eder.
    restoreSettings();

    // İlk render bittikten sonra no-transition class'ını kaldır.
    // Çift requestAnimationFrame: tarayıcının ilk frame'i çizmesini garantiler.
    requestAnimationFrame(function () {
        requestAnimationFrame(function () {
            document.documentElement.classList.remove("no-transition");
        });
    });

    // Dil dropdown dışarı tıklandığında kapat
    document.addEventListener("click", function (e) {
        var dd = document.getElementById("spLangDd");
        if (dd && dd.classList.contains("is-open") && !dd.contains(e.target)) {
            dd.classList.remove("is-open");
            var trigger = document.getElementById("spLangTrigger");
            if (trigger) trigger.setAttribute("aria-expanded", "false");
        }
    });

    // ── Arama overlay event listener'ları ──
    var searchSubmitBtn = document.querySelector(".search-submit");
    if (searchSubmitBtn) {
        searchSubmitBtn.addEventListener("click", doSearch);
    }
    var searchInpEl = document.getElementById("searchInp");
    if (searchInpEl) {
        searchInpEl.addEventListener("keypress", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                doSearch();
            }
        });
    }

    // ── Blog sıralama dropdown ──
    var sortBtn = document.getElementById("sortBtn");
    var sortWrap = document.getElementById("sortWrap");
    if (sortBtn && sortWrap) {
        sortBtn.addEventListener("click", function (e) {
            e.stopPropagation();
            sortWrap.classList.toggle("open");
            sortBtn.setAttribute(
                "aria-expanded",
                sortWrap.classList.contains("open"),
            );
        });
        document.addEventListener("click", function () {
            sortWrap.classList.remove("open");
            sortBtn.setAttribute("aria-expanded", "false");
        });
        document.querySelectorAll(".blog-sort-opt").forEach(function (opt) {
            opt.addEventListener("click", function () {
                document.querySelectorAll(".blog-sort-opt").forEach(function (o) {
                    o.classList.remove("active");
                    o.setAttribute("aria-selected", "false");
                });
                this.classList.add("active");
                this.setAttribute("aria-selected", "true");
                var label = document.getElementById("sortLabel");
                if (label) label.textContent = this.textContent.trim();
                sortWrap.classList.remove("open");
                sortBtn.setAttribute("aria-expanded", "false");
            });
        });
    }

    // ── Blog kategori filtreleme ──
    document.querySelectorAll(".blog-cat-pill").forEach(function (pill) {
        pill.addEventListener("click", function () {
            document.querySelectorAll(".blog-cat-pill").forEach(function (p) {
                p.classList.remove("active");
                p.setAttribute("aria-pressed", "false");
            });
            this.classList.add("active");
            this.setAttribute("aria-pressed", "true");
            var cat = this.dataset.cat;
            var cards = document.querySelectorAll("#blogGrid > [data-cat]");
            var count = 0;
            cards.forEach(function (card) {
                if (cat === "all" || card.dataset.cat === cat) {
                    card.style.display = "";
                    count++;
                } else {
                    card.style.display = "none";
                }
            });
            var resultEl = document.getElementById("resultCount");
            if (resultEl) resultEl.textContent = count;
            var emptyEl = document.getElementById("blogEmpty");
            if (emptyEl) emptyEl.style.display = count === 0 ? "flex" : "none";
        });
    });

    // ── Blog arama ──
    var blogSearchInp = document.getElementById("blogSearchInp");
    if (blogSearchInp) {
        blogSearchInp.addEventListener("input", function () {
            var q = this.value.trim().toLowerCase();
            var cards = document.querySelectorAll("#blogGrid > [data-cat]");
            var count = 0;
            cards.forEach(function (card) {
                var title =
                    card.querySelector(".blog-card-title") || card.querySelector("h3");
                var text = title ? title.textContent.toLowerCase() : "";
                if (text.indexOf(q) !== -1 || q === "") {
                    card.style.display = "";
                    count++;
                } else {
                    card.style.display = "none";
                }
            });
            var resultEl = document.getElementById("resultCount");
            if (resultEl) resultEl.textContent = count;
            var emptyEl = document.getElementById("blogEmpty");
            if (emptyEl) emptyEl.style.display = count === 0 ? "flex" : "none";
        });
    }

    // ── Ürün sıralama dropdown ──
    var prodSortBtn = document.getElementById("prodSortBtn");
    var prodSortWrap = document.getElementById("prodSortWrap");
    if (prodSortBtn && prodSortWrap) {
        prodSortBtn.addEventListener("click", function (e) {
            e.stopPropagation();
            prodSortWrap.classList.toggle("open");
            prodSortBtn.setAttribute(
                "aria-expanded",
                prodSortWrap.classList.contains("open"),
            );
        });
        document.addEventListener("click", function () {
            if (prodSortWrap) {
                prodSortWrap.classList.remove("open");
                prodSortBtn.setAttribute("aria-expanded", "false");
            }
        });
        document
            .querySelectorAll("#prodSortWrap .blog-sort-opt")
            .forEach(function (opt) {
                opt.addEventListener("click", function () {
                    var label = document.getElementById("prodSortLabel");
                    if (label) label.textContent = this.textContent.trim();
                    prodSortWrap.classList.remove("open");
                    prodSortBtn.setAttribute("aria-expanded", "false");
                });
            });
    }

    toastr.options = {
        positionClass: "toast-bottom-right",
        timeOut: 1600,
        progressBar: true,
        closeButton: true
    };

    // ── Canlı Arama Başlat ──
    initLiveSearch();
});
