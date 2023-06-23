# This repository is a private fork of https://github.com/dgarage/NBXplorer

Managing a private forked repository is not as simple as a normal fork.

We will need to manually add upstream and keep it updated:
```
git remote add upstream git@github.com:dgarage/NBXplorer.git

git fetch upstream
```

And keep `master` branch updated for example:
```
git merge upstream/master origin/master

git push origin/master
```