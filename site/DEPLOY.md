# MediCore website: deploy guide

## 1. Replace one placeholder (Find and Replace in all files)

Your GitHub username and LinkedIn profile are already filled in.

| Placeholder | Replace with | Example |
|---|---|---|
| `YOUR_DOMAIN` | Your site address, without `https://` and without a trailing slash | `medicore.dev` or `varunsalma.github.io/MediCore` |

Files that contain the placeholder: `index.html`, `robots.txt`, `sitemap.xml`.
In VS Code use Ctrl+Shift+H, choose "Replace in files", and replace each one.

## 2. Check before you upload
- The code samples in the Quick start section match your real MediCore API.
- Your README on GitHub is ready, because the Docs buttons open it.
- Open `index.html` in a browser and click every link.

## 3. Host it (pick one, all free)

**GitHub Pages**
1. Create a repo (for example `MediCore-site`) and upload every file in this folder.
2. Repo Settings, then Pages, then Source: "Deploy from a branch", branch `main`, folder `/ (root)`.
3. Your site appears at `https://varunsalma.github.io/MediCore-site/`.
   Use `varunsalma.github.io/MediCore-site` as `YOUR_DOMAIN`.

**Netlify**: go to app.netlify.com/drop and drag this folder onto the page.

**Vercel**: import the repo, set the framework to "Other", and deploy.

## 4. After it is live
1. Open your site URL and test on a phone.
2. Paste the URL into LinkedIn Post Inspector (linkedin.com/post-inspector) to refresh the preview card.
3. Add the site URL to the GitHub repo "About" website field and to your NuGet `PackageProjectUrl`.
4. Add the site to your LinkedIn Featured section.
5. Optional: submit `https://YOUR_DOMAIN/sitemap.xml` in Google Search Console.

## Folder contents
- `index.html`: the landing page
- `404.html`: page shown for broken links
- `assets/icon.png`: your logo (also the favicon)
- `assets/og-image.png`: 1200x630 preview image for link sharing
- `robots.txt` and `sitemap.xml`: search engine files
- `.nojekyll`: tells GitHub Pages to serve the files as they are
