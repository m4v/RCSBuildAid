#!/usr/bin/env python3

import sys
import json
import argparse
import requests
import subprocess
from os.path import basename
from changelog import ChangelogParser

project_id = 220602
project_name = "RCSBuildAid"
author = "m4v"
curse_url = f"https://kerbal.curseforge.com/api/projects/{project_id}/upload-file"
metadata = \
    {
        "gameVersions": [7999, 7944, 7772, 7771],
        "releaseType": "release"
    }


def get_token(site):
    with open(f'scripts/{site}.token') as fd:
        user, token = fd.readline().strip().split(':', 1)
    return user, token


def curse_print_game_versions():
    r = requests.get("https://kerbal.curseforge.com/api/game/versions",
                     auth=get_token('curse'))
    if r.status_code == 200:
        s = json.dumps(r.json(), indent=4, sort_keys=True)
        print(s)
    else:
        raise Exception


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument('--version', type=str)
    ap.add_argument('--file', type=str)
    ap.add_argument('--changelog', type=str)
    ap.add_argument('--description', type=str)
    ap.add_argument('--github', action='store_true', default=False)
    ap.add_argument('--curse', action='store_true', default=False)
    ap.add_argument('--release', action='store_true', default=False)
    ap.add_argument('--curse-ksp-versions', action='store_true', default=False)
    args = ap.parse_args()

    if args.curse_ksp_versions:
        curse_print_game_versions()
        sys.exit(0)
    else:
        args_ok = True
        if not args.version:
            print("Missing version argument.")
            args_ok = False
        if not args.file:
            print("Missing file to upload.")
            args_ok = False
        if not (args.changelog or args.description):
            print("Missing changelog.")
            args_ok = False
        if not (args.curse or args.github):
            print("Missing site to release.")
            args_ok = False

    if not args_ok:
        sys.exit(-1)

    changelog = ''
    if args.changelog:
        parser = ChangelogParser(args.version)
        parser.feed(args.changelog)
        if not parser.changelog:
            print("Couldn't find a changelog for {}".format(args.version))
            print("aborting release")
            sys.exit(-1)
        changelog = parser.changelog
    if args.description:
        changelog = args.description
    if not changelog:
        print("Missing changelog.")
        sys.exit(-1)

    if args.curse:
        metadata["changelog"] = changelog
        metadata["changelogType"] = "text"
        metadata_json = json.dumps(metadata)
        data = {'metadata': metadata_json}
        if args.release:
            files = {'file': (basename(args.file), open(args.file, 'rb'))}
            r = requests.post(curse_url, auth=get_token('curse'), data=data, files=files)
            if r.status_code != 200:
                sys.exit(-1)
        else:
            files = {'file': (basename(args.file), None)}
            print(metadata['changelog'])
            print("\nPayload:")
            print(json.dumps((data, files), indent=4))

    if args.github:
        token = get_token('github')[1]
        arguments = [
            'github-release', 'release',
            '--user', author,
            '--repo', project_name,
            '--tag', args.version,
            '--security-token', token,
            '--name', f"version {args.version.lstrip('v')}",
            '--description', changelog,
        ]
        if not args.release:
            arguments.append('--draft')
        subprocess.run(arguments)
        arguments = [
            'github-release', 'upload',
            '--user', author,
            '--repo', project_name,
            '--tag', args.version,
            '--security-token', token,
            '--name', basename(args.file),
            '--file', args.file,
        ]
        subprocess.run(arguments)
