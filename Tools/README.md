# Report generation

Run `python3 Tools/BuildExpansionReport.py` from the Unity project after updating the Markdown design/report and validation JSON. Dependencies: reportlab, Pillow, pypdf. The script reads existing scene/test evidence and never runs or invents human tests. Output: `output/pdf/KombiShift2_Expansion_Report_2026-11-10.pdf`. Render with Poppler (`pdftoppm -png`) and visually inspect every page before distributing a regenerated PDF.
