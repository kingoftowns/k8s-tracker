{{/*
Expand the name of the chart.
*/}}
{{- define "k8s-tracker-backend.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "k8s-tracker-backend.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "k8s-tracker-backend.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "k8s-tracker-backend.labels" -}}
helm.sh/chart: {{ include "k8s-tracker-backend.chart" . }}
{{ include "k8s-tracker-backend.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/part-of: k8s-tracker
{{- end }}

{{/*
Selector labels
*/}}
{{- define "k8s-tracker-backend.selectorLabels" -}}
app.kubernetes.io/name: {{ include "k8s-tracker-backend.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the database connection string
*/}}
{{- define "k8s-tracker-backend.connectionString" -}}
{{- printf "Host=%s;Database=%s;Username=%s;Password=%s" .Values.database.host .Values.database.name .Values.database.username .Values.database.password }}
{{- end }} 