#!/usr/bin/env python3

import re
import sys
import argparse


class ChangelogParser(object):
    def __init__(self, version):
        self._version_re = re.compile(r'^=+ Version {}$'.format(self._make_version_re(version)))
        self._any_version_re = re.compile(r'^=+ Version [0-9.]+$')
        self._bullet_re = re.compile(r'^\s*\*+\s.*$')
        self._changelog = []
        self._state = 'INIT'

    @staticmethod
    def _make_version_re(s):
        s = s.lstrip('v')
        return s.replace('.', r'\.')

    def feed(self, path):
        if not path:
            return

        with open(path, 'r') as fd:
            for line in fd:
                if self._state != 'END':
                    self._parse_line(line)
                else:
                    break

    def _parse_line(self, line):
        if self._state == 'INIT':
            if self._version_re.match(line):
                self._changelog.append(line)
                self._changelog.append('\n')
                self._state = 'BULLET'
        elif self._state == 'BULLET':
            if self._bullet_re.match(line):
                self._changelog.append(line)
                self._state = 'ALL'
            if self._any_version_re.match(line):
                self._state = 'END'
        elif self._state == 'ALL':
            if line.strip() == '':
                self._state = 'BULLET'
            else:
                self._changelog.append(line)

    @property
    def changelog(self):
        return ''.join(self._changelog).strip()


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument('version', type=str)
    ap.add_argument('changelog', type=str)
    args = ap.parse_args()

    parser = ChangelogParser(args.version)
    parser.feed(args.changelog)
    if not parser.changelog:
        print("Couldn't find a changelog for {}".format(args.version))
        sys.exit(-1)
    else:
        print(parser.changelog)
