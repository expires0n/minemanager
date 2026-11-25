"""Checks that every literal L.T("...") / L.F("...") text in src/ has a Turkish entry.

English is the source language and a missing key silently falls back to English, so a
key that differs from its call site (for example a space where the code has "\\n") is
easy to miss. This script compares them exactly and also catches a key registered twice
in one dictionary initializer, which would throw when the app starts.

Usage: python tools/check_translations.py   (exit code 1 when something is wrong)
"""
import collections
import glob
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "src")
LIT = r'"(?:[^"\\\n]|\\.)*"'
CONCAT = r'((?:' + LIT + r'\s*\+?\s*)+)'

# Texts with nothing to translate (product names around a placeholder).
ALLOW = {"MSI {0}: ", "Windows: {0}"}


def unescape(s):
    return s[1:-1].replace('\\n', '\n').replace('\\t', '\t').replace('\\"', '"').replace('\\\\', '\\')


def join(chunk):
    return ''.join(unescape(m) for m in re.findall(LIT, chunk))


def read(path):
    with open(path, encoding='utf-8-sig') as f:
        return f.read()


def main():
    os.chdir(ROOT)
    keys = set()
    problems = []
    for path in sorted(glob.glob('Lang*.cs')):
        text = read(path)
        seen = collections.Counter()
        for m in re.finditer(r'\{\s*' + CONCAT + r',\s*' + CONCAT + r'\}', text):
            key = join(m.group(1))
            keys.add(key)
            seen[key] += 1
        for m in re.finditer(r'd\[\s*' + CONCAT + r'\]\s*=\s*' + CONCAT + r';', text):
            keys.add(join(m.group(1)))
        for key, count in seen.items():
            if count > 1:
                problems.append('%s: key registered %d times: %r' % (path, count, key))

    for path in sorted(glob.glob('**/*.cs', recursive=True)):
        if os.path.basename(path).startswith('Lang'):
            continue
        text = read(path)
        for m in re.finditer(r'\bL\.(?:T|F)\(\s*' + CONCAT + r'(?=[,)])', text):
            s = join(m.group(1))
            if s in keys or s in ALLOW or not re.search('[A-Za-z]{2}', s):
                continue
            line = text[:m.start()].count('\n') + 1
            problems.append('%s:%d: no Turkish entry for %r' % (path.replace(os.sep, '/'), line, s))

    for p in problems:
        print(p)
    print('%d translation keys, %d problem(s)' % (len(keys), len(problems)))
    return 1 if problems else 0


if __name__ == '__main__':
    sys.exit(main())
