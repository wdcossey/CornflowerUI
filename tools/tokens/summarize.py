"""Counts the entries of a themeresources.xaml by element type.

Prints one line per theme dictionary (Default, Light, HighContrast...) and one for the entries outside
them: how many there are and how many of each element type (Color, SolidColorBrush, Double, Style and
so on). A quick look at what a version of the file holds, before or after running gen.py.

    python summarize.py <path to themeresources.xaml>
"""
import argparse
import collections
import os
import sys
import xml.etree.ElementTree as ET

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from resources import X, local  # noqa: E402

ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
ap.add_argument('path', help='a themeresources.xaml')
root = ET.parse(ap.parse_args().path).getroot()


def summarize(d, label):
    c = collections.Counter(local(e.tag) for e in d)
    print(label, len(list(d)), dict(c))


for child in root:
    if local(child.tag) == 'ResourceDictionary.ThemeDictionaries':
        for td in child:
            summarize(td, 'theme ' + td.get(X + 'Key'))
others = [e for e in root if local(e.tag) != 'ResourceDictionary.ThemeDictionaries']
summarize(others, 'top-level')
