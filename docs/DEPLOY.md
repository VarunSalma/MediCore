# Deploy the MediCore website on GitHub Pages

Your site will be live at: https://varunsalma.github.io/MediCore/

## Steps
1. Open your local MediCore repo folder (the one connected to github.com/VarunSalma/MediCore).
2. If you already have a `docs` folder, delete or rename it. Note that GitHub Pages can only publish the repo root or a folder named `docs`. If your current page is in another folder, move on to step 3 anyway, and delete the old page once the new one works.
3. Copy the `docs` folder from this zip into the root of the repo, so you have `MediCore/docs/index.html`.
4. In a terminal, run:
   ```
   git add docs
   git commit -m "Add MediCore website"
   git push
   ```
5. On GitHub open the repo, then Settings, then Pages.
6. Under "Build and deployment", set Source to "Deploy from a branch".
7. Set Branch to `main` (or your default branch) and the folder to `/docs`, then click Save.
8. Wait 1 to 2 minutes and open https://varunsalma.github.io/MediCore/.

## After it is live
- Test on your phone and click every link.
- Refresh the LinkedIn preview: linkedin.com/post-inspector
- Repo home page: click the gear next to "About" and set Website to the site URL.
- Update `PackageProjectUrl` in your `.csproj` to the site URL and publish the next version.
- Add the site to your LinkedIn Featured section.
- Optional: submit https://varunsalma.github.io/MediCore/sitemap.xml in Google Search Console.

## If something looks wrong
- 404 error: check Settings, then Pages, shows `main` and `/docs`, and that `docs/index.html` exists on GitHub.
- Old page still showing: hard refresh with Ctrl+F5.
- Logo missing: the file must be at `docs/assets/icon.png`.
