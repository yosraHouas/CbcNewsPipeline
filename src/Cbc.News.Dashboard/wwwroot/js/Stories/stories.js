function getAuthHeaders() {
    const token = window.authConfig?.token;

    if (!token) {
        return {};
    }

    return {
        "Authorization": `Bearer ${token}`
    };
}
document.addEventListener("DOMContentLoaded", () => {
    const applyBtn = document.getElementById("applyStoriesFilterBtn");
    const feedFilter = document.getElementById("feedFilter");
    const limitFilter = document.getElementById("limitFilter");

    if (applyBtn) {
        applyBtn.addEventListener("click", loadStories);
    }

    if (feedFilter) {
        feedFilter.value = "";
    }

    loadStories();
});

async function loadStories() {
    const grid = document.getElementById("storiesGrid");
    const feedFilter = document.getElementById("feedFilter");
    const limitFilter = document.getElementById("limitFilter");

    if (!grid) return;

    grid.innerHTML = `<div class="col-12 text-muted">Loading stories...</div>`;

    try {
        const baseUrl = new URL(window.dashboardConfig.storiesBaseUrl || window.dashboardConfig.storiesUrl);

        const feed = feedFilter?.value ?? "";
        const limit = limitFilter?.value ?? "20";

        baseUrl.searchParams.delete("feed");
        baseUrl.searchParams.delete("limit");

        if (feed) {
            baseUrl.searchParams.set("feed", feed);
        }

        baseUrl.searchParams.set("limit", limit);

        console.log("storiesUrl =", baseUrl.toString());

        const response = await fetch(baseUrl.toString(), {
            headers: getAuthHeaders()
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const data = await response.json();
        console.log("stories response =", data);

        const items = Array.isArray(data) ? data : (data.items ?? []);

        grid.innerHTML = "";

        if (!items.length) {
            grid.innerHTML = `
                <div class="col-12">
                    <div class="alert alert-light border">No stories found.</div>
                </div>`;
            return;
        }

        items.forEach(story => {
            const fallbackImage = extractImageFromSummary(story.summary);
            const imageUrl =
                story.imageUrl && story.imageUrl.trim() !== ""
                    ? story.imageUrl
                    : (fallbackImage || "https://placehold.co/600x400?text=CBC+News");

            const publishedValue = story.publishedAtUtc ?? story.publishedAt ?? "";
            const publishedText = publishedValue
                ? new Date(publishedValue).toLocaleString()
                : "";

            const summaryText = truncate(stripHtml(story.summary ?? ""), 180);

            const card = document.createElement("div");
            card.className = "col-md-6 col-lg-4";

            card.innerHTML = `
                <div class="card h-100 shadow-sm border-0">
                    <img src="${escapeHtml(imageUrl)}"
                         class="card-img-top"
                         alt="${escapeHtml(story.title ?? "Story image")}"
                         style="height:220px;object-fit:cover;"
                         onerror="this.src='https://placehold.co/600x400?text=CBC+News'">

                    <div class="card-body d-flex flex-column">
                        <div class="mb-2">
                            <span class="badge text-bg-primary">${escapeHtml(story.feed ?? "")}</span>
                        </div>

                        <h5 class="card-title">${escapeHtml(story.title ?? "")}</h5>

                        <p class="card-text text-muted small mb-2">
                            ${escapeHtml(publishedText)}
                        </p>

                        <p class="card-text text-muted flex-grow-1">
                            ${escapeHtml(summaryText)}
                        </p>

                        <div class="mt-auto">
                            <a href="${escapeHtml(story.url ?? "#")}"
                               target="_blank"
                               rel="noopener noreferrer"
                               class="btn btn-outline-primary btn-sm">
                                Read article
                            </a>
                        </div>
                    </div>
                </div>
            `;

            grid.appendChild(card);
        });
    } catch (error) {
        console.error("Failed to load stories:", error);

        grid.innerHTML = `
            <div class="col-12">
                <div class="alert alert-danger">Unable to load stories.</div>
            </div>`;
    }
}

function extractImageFromSummary(summary) {
    if (!summary) return "";
    const match = summary.match(/<img[^>]+src=['"]([^'"]+)['"]/i);
    return match ? match[1] : "";
}

function stripHtml(value) {
    const temp = document.createElement("div");
    temp.innerHTML = value || "";
    return temp.textContent || temp.innerText || "";
}

function truncate(value, maxLength) {
    if (!value || value.length <= maxLength) return value;
    return value.substring(0, maxLength).trim() + "...";
}

function escapeHtml(value) {
    if (!value) return "";
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}